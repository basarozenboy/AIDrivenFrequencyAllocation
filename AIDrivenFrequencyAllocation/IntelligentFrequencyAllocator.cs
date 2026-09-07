namespace AIDrivenFrequencyAllocation
{
    public class IntelligentFrequencyAllocator
    {
        private double minFreq; // GHz
        private double maxFreq; // GHz
        private List<AllocatedBlock> allocatedBlocks;
        private Random random;
        private double guardBand = 0.01; // 10 MHz guard band

        // ML parametreleri
        private double learningRate = 0.1;
        private Dictionary<double, double> fragmentationHistory;
        private double temperature = 100.0;
        private double coolingRate = 0.95;

        public IntelligentFrequencyAllocator(double minFreqGHz, double maxFreqGHz)
        {
            minFreq = minFreqGHz;
            maxFreq = maxFreqGHz;
            allocatedBlocks = new List<AllocatedBlock>();
            random = new Random();
            fragmentationHistory = new Dictionary<double, double>();
        }

        public double MinFrequency => minFreq;
        public double MaxFrequency => maxFreq;

        // TOPLU TAHSİS - Frekansları birbirlerine maksimum uzaklıkta yerleştir
        public BatchAllocationResult AllocateBatch(List<FrequencyAllocationRequest> requests)
        {
            var result = new BatchAllocationResult
            {
                SuccessfulAllocations = new List<AllocatedBlock>(),
                FailedRequests = new List<FrequencyAllocationRequest>(),
                TotalFragmentation = 0,
                OptimizationScore = 0
            };

            if (requests == null || requests.Count == 0)
                return result;

            // Geçersiz istekleri filtrele
            var validRequests = new List<FrequencyAllocationRequest>();
            foreach (var request in requests)
            {
                if (request.Bandwidth > 0 && request.Bandwidth <= (maxFreq - minFreq))
                {
                    validRequests.Add(request);
                }
                else
                {
                    result.FailedRequests.Add(request);
                }
            }

            if (validRequests.Count == 0)
                return result;

            // Bant genişliğine göre sırala (büyükten küçüğe - büyük bantlar önce yerleştirilir)
            validRequests = validRequests.OrderByDescending(r => r.Bandwidth).ToList();

            // Geçici tahsis listesi (henüz kalıcı değil)
            var tempAllocations = new List<AllocatedBlock>();
            var tempAllocatedBlocks = new List<AllocatedBlock>(allocatedBlocks);

            // Her istek için optimal pozisyon bul
            foreach (var request in validRequests)
            {
                var position = FindOptimalBatchPosition(request.Bandwidth, tempAllocatedBlocks);

                if (position.HasValue)
                {
                    var allocation = new AllocatedBlock
                    {
                        CenterFrequency = position.Value,
                        Bandwidth = request.Bandwidth,
                        AllocationId = request.RequestId ?? Guid.NewGuid().ToString(),
                        AllocationTime = DateTime.Now
                    };

                    tempAllocations.Add(allocation);
                    tempAllocatedBlocks.Add(allocation);
                }
                else
                {
                    result.FailedRequests.Add(request);
                }
            }

            // Tüm tahsisleri optimize et (maksimum uzaklık için)
            var optimizedAllocations = OptimizeBatchDistribution(tempAllocations, tempAllocatedBlocks);

            // Kalıcı olarak ekle
            allocatedBlocks.AddRange(optimizedAllocations);
            result.SuccessfulAllocations = optimizedAllocations;

            // Fragmentasyon ve optimizasyon skorunu hesapla
            result.TotalFragmentation = CalculateTotalFragmentationForBatch();
            result.OptimizationScore = CalculateOptimizationScore(optimizedAllocations);

            return result;
        }

        // Batch için optimal pozisyon bul
        private double? FindOptimalBatchPosition(double bandwidth, List<AllocatedBlock> currentBlocks)
        {
            var gaps = FindAvailableGapsWithBlocks(bandwidth, currentBlocks);

            if (gaps.Count == 0)
                return null;

            // En geniş boşluğu seç
            var largestGap = gaps.OrderByDescending(g => g.Item2 - g.Item1).First();

            // Bu boşlukta maksimum uzaklık noktasını hesapla
            double optimalPosition = CalculateMaxDistancePointWithBlocks(
                largestGap.Item1,
                largestGap.Item2,
                bandwidth,
                currentBlocks
            );

            return optimalPosition;
        }

        // Belirli blok listesiyle boşlukları bul
        private List<Tuple<double, double>> FindAvailableGapsWithBlocks(double bandwidth, List<AllocatedBlock> blocks)
        {
            var gaps = new List<Tuple<double, double>>();
            var sorted = blocks.OrderBy(b => b.StartFreq).ToList();

            if (sorted.Count == 0)
            {
                gaps.Add(new Tuple<double, double>(minFreq, maxFreq));
                return gaps;
            }

            if (sorted[0].StartFreq - minFreq >= bandwidth + guardBand * 2)
            {
                gaps.Add(new Tuple<double, double>(minFreq, sorted[0].StartFreq - guardBand));
            }

            for (int i = 0; i < sorted.Count - 1; i++)
            {
                double gapStart = sorted[i].EndFreq + guardBand;
                double gapEnd = sorted[i + 1].StartFreq - guardBand;

                if (gapEnd - gapStart >= bandwidth)
                {
                    gaps.Add(new Tuple<double, double>(gapStart, gapEnd));
                }
            }

            if (sorted.Count > 0 && maxFreq - sorted.Last().EndFreq >= bandwidth + guardBand * 2)
            {
                gaps.Add(new Tuple<double, double>(sorted.Last().EndFreq + guardBand, maxFreq));
            }

            return gaps;
        }

        // Belirli blok listesiyle maksimum uzaklık noktası hesapla
        private double CalculateMaxDistancePointWithBlocks(double gapStart, double gapEnd, double bandwidth, List<AllocatedBlock> blocks)
        {
            double halfBandwidth = bandwidth / 2;

            double searchStart = gapStart + halfBandwidth;
            double searchEnd = gapEnd - halfBandwidth;

            if (searchEnd <= searchStart)
                return (gapStart + gapEnd) / 2;

            if (blocks.Count <= 1)
                return (gapStart + gapEnd) / 2;

            int sampleCount = 100;
            double step = (searchEnd - searchStart) / sampleCount;

            double bestPosition = searchStart;
            double maxMinDistance = 0;

            for (int i = 0; i <= sampleCount; i++)
            {
                double candidatePos = searchStart + (i * step);

                double minDistance = double.MaxValue;

                foreach (var block in blocks)
                {
                    double distance = Math.Abs(candidatePos - block.CenterFrequency);
                    minDistance = Math.Min(minDistance, distance);
                }

                if (minDistance > maxMinDistance)
                {
                    maxMinDistance = minDistance;
                    bestPosition = candidatePos;
                }
            }

            return bestPosition;
        }

        // Batch dağılımını optimize et (tüm frekanslar arası mesafeleri maksimize et)
        private List<AllocatedBlock> OptimizeBatchDistribution(List<AllocatedBlock> newAllocations, List<AllocatedBlock> allBlocks)
        {
            if (newAllocations.Count <= 1)
                return newAllocations;

            // İteratif optimizasyon (her tahsisi diğerlerine göre ayarla)
            int maxIterations = 3;
            var optimized = new List<AllocatedBlock>(newAllocations);

            for (int iteration = 0; iteration < maxIterations; iteration++)
            {
                bool improved = false;

                for (int i = 0; i < optimized.Count; i++)
                {
                    var current = optimized[i];

                    // Bu tahsis için mevcut tüm bloklar (kendisi hariç)
                    var otherBlocks = allBlocks.Where(b => b.AllocationId != current.AllocationId).ToList();

                    // Mevcut pozisyondan daha iyi bir pozisyon var mı?
                    var betterPosition = FindBetterPositionForBlock(current, otherBlocks);

                    if (betterPosition.HasValue && Math.Abs(betterPosition.Value - current.CenterFrequency) > 0.01)
                    {
                        // Pozisyonu güncelle
                        optimized[i] = new AllocatedBlock
                        {
                            CenterFrequency = betterPosition.Value,
                            Bandwidth = current.Bandwidth,
                            AllocationId = current.AllocationId,
                            AllocationTime = current.AllocationTime
                        };

                        // allBlocks'ta da güncelle
                        var indexInAll = allBlocks.FindIndex(b => b.AllocationId == current.AllocationId);
                        if (indexInAll >= 0)
                        {
                            allBlocks[indexInAll] = optimized[i];
                        }

                        improved = true;
                    }
                }

                if (!improved)
                    break; // Optimizasyon tamamlandı
            }

            return optimized;
        }

        // Bir blok için daha iyi pozisyon ara
        private double? FindBetterPositionForBlock(AllocatedBlock block, List<AllocatedBlock> otherBlocks)
        {
            var gaps = FindAvailableGapsWithBlocks(block.Bandwidth, otherBlocks);

            if (gaps.Count == 0)
                return null;

            double currentMinDistance = CalculateMinDistanceToOthers(block.CenterFrequency, otherBlocks);
            double bestPosition = block.CenterFrequency;
            double bestMinDistance = currentMinDistance;

            foreach (var gap in gaps)
            {
                // Bu boşlukta en iyi pozisyonu bul
                double candidatePos = CalculateMaxDistancePointWithBlocks(gap.Item1, gap.Item2, block.Bandwidth, otherBlocks);
                double candidateMinDistance = CalculateMinDistanceToOthers(candidatePos, otherBlocks);

                if (candidateMinDistance > bestMinDistance)
                {
                    bestMinDistance = candidateMinDistance;
                    bestPosition = candidatePos;
                }
            }

            return Math.Abs(bestPosition - block.CenterFrequency) > 0.01 ? bestPosition : null;
        }

        // Bir pozisyondan diğer bloklara minimum mesafe
        private double CalculateMinDistanceToOthers(double position, List<AllocatedBlock> otherBlocks)
        {
            if (otherBlocks.Count == 0)
                return double.MaxValue;

            double minDistance = double.MaxValue;
            foreach (var block in otherBlocks)
            {
                double distance = Math.Abs(position - block.CenterFrequency);
                minDistance = Math.Min(minDistance, distance);
            }

            return minDistance;
        }

        // Toplam fragmentasyon hesapla
        private double CalculateTotalFragmentationForBatch()
        {
            if (allocatedBlocks.Count < 2)
                return 0;

            var sorted = allocatedBlocks.OrderBy(b => b.StartFreq).ToList();
            double totalFragmentation = 0;
            int fragmentCount = 0;

            // Başlangıç boşluğu
            if (sorted[0].StartFreq - minFreq > guardBand)
            {
                double gapSize = sorted[0].StartFreq - minFreq;
                totalFragmentation += gapSize;
                fragmentCount++;
            }

            // Ara boşluklar
            for (int i = 0; i < sorted.Count - 1; i++)
            {
                double gapSize = sorted[i + 1].StartFreq - sorted[i].EndFreq;
                if (gapSize > guardBand)
                {
                    totalFragmentation += gapSize;
                    fragmentCount++;
                }
            }

            // Son boşluk
            if (maxFreq - sorted.Last().EndFreq > guardBand)
            {
                double gapSize = maxFreq - sorted.Last().EndFreq;
                totalFragmentation += gapSize;
                fragmentCount++;
            }

            // Ortalama boşluk boyutunu döndür (düşük = daha az fragmentasyon)
            return fragmentCount > 0 ? totalFragmentation / fragmentCount : 0;
        }

        // Optimizasyon skoru hesapla (0-100, yüksek = daha iyi)
        private double CalculateOptimizationScore(List<AllocatedBlock> newAllocations)
        {
            if (newAllocations.Count == 0)
                return 0;

            double score = 100.0;

            // 1. Minimum mesafe skoru (tahsisler arası en küçük mesafe ne kadar büyük?)
            if (allocatedBlocks.Count >= 2)
            {
                var sorted = allocatedBlocks.OrderBy(b => b.CenterFrequency).ToList();
                double minDistance = double.MaxValue;

                for (int i = 0; i < sorted.Count - 1; i++)
                {
                    double distance = sorted[i + 1].CenterFrequency - sorted[i].CenterFrequency;
                    minDistance = Math.Min(minDistance, distance);
                }

                // Minimum mesafe skoru (0-40 puan)
                double idealMinDistance = (maxFreq - minFreq) / allocatedBlocks.Count;
                double distanceRatio = Math.Min(minDistance / idealMinDistance, 1.0);
                score -= (1.0 - distanceRatio) * 40;
            }

            // 2. Spektrum kullanım verimliliği (20 puan)
            double utilization = GetSpectrumUtilization();
            if (utilization > 90)
                score -= 20; // Aşırı dolu
            else if (utilization < 10)
                score -= 10; // Çok boş

            // 3. Fragmentasyon skoru (0-30 puan)
            double avgFragmentation = CalculateTotalFragmentation();
            double totalSpectrum = maxFreq - minFreq;
            double fragmentationRatio = avgFragmentation / totalSpectrum;
            if (fragmentationRatio > 0.1) // Ortalama boşluk %10'dan büyükse
            {
                score -= fragmentationRatio * 100;
            }

            // 4. Uniform dağılım bonusu (0-10 puan)
            double uniformity = CalculateUniformityScore();
            score -= (1.0 - uniformity) * 10;

            return Math.Max(0, Math.Min(100, score));
        }

        // Uniform dağılım skoru (0-1, 1 = mükemmel uniform)
        private double CalculateUniformityScore()
        {
            if (allocatedBlocks.Count < 3)
                return 1.0;

            var sorted = allocatedBlocks.OrderBy(b => b.CenterFrequency).ToList();
            var distances = new List<double>();

            for (int i = 0; i < sorted.Count - 1; i++)
            {
                distances.Add(sorted[i + 1].CenterFrequency - sorted[i].CenterFrequency);
            }

            if (distances.Count == 0)
                return 1.0;

            // Standart sapma hesapla
            double mean = distances.Average();
            double variance = distances.Sum(d => Math.Pow(d - mean, 2)) / distances.Count;
            double stdDev = Math.Sqrt(variance);

            // Düşük standart sapma = yüksek uniformity
            double cv = mean > 0 ? stdDev / mean : 0; // Coefficient of variation
            return Math.Exp(-cv); // 0 ile 1 arası normalize et
        }

        // PSO ile optimal toplu tahsis bulma
        private List<AllocatedBlock> FindOptimalBatchAllocation(List<FrequencyAllocationRequest> requests)
        {
            int swarmSize = 30;
            int maxIterations = 100;
            var particles = new List<Particle>();

            // Swarm'ı başlat
            for (int i = 0; i < swarmSize; i++)
            {
                var particle = CreateRandomParticle(requests);
                particles.Add(particle);
            }

            Particle globalBest = particles.OrderBy(p => p.Fitness).First();

            // PSO iterasyonları
            for (int iter = 0; iter < maxIterations; iter++)
            {
                foreach (var particle in particles)
                {
                    // Hız güncelleme
                    UpdateParticleVelocity(particle, globalBest);

                    // Pozisyon güncelleme
                    UpdateParticlePosition(particle, requests);

                    // Fitness hesapla
                    particle.Fitness = CalculateParticleFitness(particle);

                    // Personal best güncelle
                    if (particle.Fitness < particle.PersonalBestFitness)
                    {
                        particle.PersonalBest = particle.Position.ToList();
                        particle.PersonalBestFitness = particle.Fitness;
                    }
                }

                // Global best güncelle
                var currentBest = particles.OrderBy(p => p.Fitness).First();
                if (currentBest.Fitness < globalBest.Fitness)
                {
                    globalBest = currentBest.Clone();
                }

                // Erken durdurma kriteri
                if (globalBest.Fitness < 0.1)
                    break;
            }

            return ConvertParticleToAllocations(globalBest, requests);
        }

        // Parçacık sınıfı
        private class Particle
        {
            public List<double> Position { get; set; } // Her talep için merkez frekans
            public List<double> Velocity { get; set; }
            public List<double> PersonalBest { get; set; }
            public double Fitness { get; set; }
            public double PersonalBestFitness { get; set; }
            public List<bool> ValidPositions { get; set; } // Geçerli pozisyonlar

            public Particle Clone()
            {
                return new Particle
                {
                    Position = Position.ToList(),
                    Velocity = Velocity.ToList(),
                    PersonalBest = PersonalBest.ToList(),
                    Fitness = Fitness,
                    PersonalBestFitness = PersonalBestFitness,
                    ValidPositions = ValidPositions.ToList()
                };
            }
        }

        // Rastgele parçacık oluştur
        private Particle CreateRandomParticle(List<FrequencyAllocationRequest> requests)
        {
            var particle = new Particle
            {
                Position = new List<double>(),
                Velocity = new List<double>(),
                ValidPositions = new List<bool>()
            };

            var tempAllocations = new List<AllocatedBlock>();

            foreach (var request in requests)
            {
                // Mevcut tahsisler + geçici tahsisler ile uyumlu pozisyon bul
                var validRange = FindValidRangeForRequest(request.Bandwidth, tempAllocations);

                if (validRange != null)
                {
                    double centerFreq = validRange.Item1 + (validRange.Item2 - validRange.Item1) * random.NextDouble();
                    particle.Position.Add(centerFreq);
                    particle.ValidPositions.Add(true);

                    tempAllocations.Add(new AllocatedBlock
                    {
                        CenterFrequency = centerFreq,
                        Bandwidth = request.Bandwidth,
                        AllocationId = request.RequestId
                    });
                }
                else
                {
                    particle.Position.Add(minFreq + (maxFreq - minFreq) * random.NextDouble());
                    particle.ValidPositions.Add(false);
                }

                particle.Velocity.Add((random.NextDouble() - 0.5) * 0.1);
            }

            particle.PersonalBest = particle.Position.ToList();
            particle.Fitness = CalculateParticleFitness(particle);
            particle.PersonalBestFitness = particle.Fitness;

            return particle;
        }

        // Geçerli aralık bulma
        private Tuple<double, double> FindValidRangeForRequest(double bandwidth, List<AllocatedBlock> tempAllocations)
        {
            var allBlocks = allocatedBlocks.Concat(tempAllocations).OrderBy(b => b.StartFreq).ToList();

            // İlk boşluk
            if (allBlocks.Count == 0 || allBlocks[0].StartFreq - minFreq >= bandwidth + guardBand * 2)
            {
                double start = minFreq + bandwidth / 2;
                double end = allBlocks.Count > 0 ? allBlocks[0].StartFreq - guardBand - bandwidth / 2 : maxFreq - bandwidth / 2;
                if (end > start)
                    return new Tuple<double, double>(start, end);
            }

            // Ara boşluklar
            for (int i = 0; i < allBlocks.Count - 1; i++)
            {
                double gapStart = allBlocks[i].EndFreq + guardBand + bandwidth / 2;
                double gapEnd = allBlocks[i + 1].StartFreq - guardBand - bandwidth / 2;

                if (gapEnd > gapStart && gapEnd - gapStart >= 0)
                {
                    return new Tuple<double, double>(gapStart, gapEnd);
                }
            }

            // Son boşluk
            if (allBlocks.Count > 0)
            {
                double start = allBlocks.Last().EndFreq + guardBand + bandwidth / 2;
                double end = maxFreq - bandwidth / 2;
                if (end > start)
                    return new Tuple<double, double>(start, end);
            }

            return null;
        }

        // Parçacık hızı güncelleme
        private void UpdateParticleVelocity(Particle particle, Particle globalBest)
        {
            double w = 0.7; // Atalet ağırlığı
            double c1 = 1.5; // Kişisel öğrenme faktörü
            double c2 = 1.5; // Sosyal öğrenme faktörü

            for (int i = 0; i < particle.Velocity.Count; i++)
            {
                double r1 = random.NextDouble();
                double r2 = random.NextDouble();

                particle.Velocity[i] = w * particle.Velocity[i] +
                                      c1 * r1 * (particle.PersonalBest[i] - particle.Position[i]) +
                                      c2 * r2 * (globalBest.Position[i] - particle.Position[i]);

                // Hız sınırlama
                particle.Velocity[i] = Math.Max(-0.5, Math.Min(0.5, particle.Velocity[i]));
            }
        }

        // Parçacık pozisyonu güncelleme
        private void UpdateParticlePosition(Particle particle, List<FrequencyAllocationRequest> requests)
        {
            var tempAllocations = new List<AllocatedBlock>();

            for (int i = 0; i < particle.Position.Count; i++)
            {
                particle.Position[i] += particle.Velocity[i];

                // Sınırları kontrol et
                double halfBand = requests[i].Bandwidth / 2;
                particle.Position[i] = Math.Max(minFreq + halfBand,
                                      Math.Min(maxFreq - halfBand, particle.Position[i]));

                // Geçerliliği kontrol et
                var block = new AllocatedBlock
                {
                    CenterFrequency = particle.Position[i],
                    Bandwidth = requests[i].Bandwidth,
                    AllocationId = requests[i].RequestId
                };

                particle.ValidPositions[i] = IsValidAllocation(block, tempAllocations);

                if (particle.ValidPositions[i])
                {
                    tempAllocations.Add(block);
                }
            }
        }

        // Tahsis geçerliliği kontrolü
        private bool IsValidAllocation(AllocatedBlock newBlock, List<AllocatedBlock> tempAllocations)
        {
            var allBlocks = allocatedBlocks.Concat(tempAllocations);

            foreach (var block in allBlocks)
            {
                if (!(newBlock.EndFreq + guardBand <= block.StartFreq ||
                      newBlock.StartFreq - guardBand >= block.EndFreq))
                {
                    return false;
                }
            }

            return newBlock.StartFreq >= minFreq && newBlock.EndFreq <= maxFreq;
        }

        // Parçacık fitness fonksiyonu
        private double CalculateParticleFitness(Particle particle)
        {
            double fitness = 0;
            int validCount = particle.ValidPositions.Count(v => v);

            // Geçersiz pozisyon cezası
            fitness += (particle.ValidPositions.Count - validCount) * 1000;

            if (validCount == 0)
                return fitness;

            // Fragmentasyon cezası
            var tempBlocks = new List<AllocatedBlock>();
            for (int i = 0; i < particle.Position.Count; i++)
            {
                if (particle.ValidPositions[i])
                {
                    tempBlocks.Add(new AllocatedBlock
                    {
                        CenterFrequency = particle.Position[i],
                        Bandwidth = 0.02, // Örnek değer
                        AllocationId = i.ToString()
                    });
                }
            }

            fitness += CalculateBatchFragmentation(tempBlocks) * 100;

            // Spektrum verimliliği (bitişik blokları tercih et)
            tempBlocks = tempBlocks.OrderBy(b => b.CenterFrequency).ToList();
            for (int i = 0; i < tempBlocks.Count - 1; i++)
            {
                double gap = tempBlocks[i + 1].StartFreq - tempBlocks[i].EndFreq;
                if (gap > guardBand * 2 && gap < 0.05) // Küçük boşluk cezası
                {
                    fitness += gap * 500;
                }
            }

            // Spektrum kullanım dengesi
            double centerPoint = (minFreq + maxFreq) / 2;
            double deviation = tempBlocks.Average(b => Math.Abs(b.CenterFrequency - centerPoint));
            fitness += deviation * 10;

            return fitness;
        }

        // Toplu fragmentasyon hesaplama
        private double CalculateBatchFragmentation(List<AllocatedBlock> newBlocks)
        {
            var allBlocks = allocatedBlocks.Concat(newBlocks).OrderBy(b => b.StartFreq).ToList();
            double fragmentation = 0;
            double totalGaps = 0;
            double usableGaps = 0;

            for (int i = 0; i < allBlocks.Count - 1; i++)
            {
                double gap = allBlocks[i + 1].StartFreq - allBlocks[i].EndFreq - guardBand * 2;
                if (gap > 0)
                {
                    totalGaps += gap;
                    if (gap >= 0.01) // Minimum kullanılabilir boşluk
                    {
                        usableGaps += gap;
                    }
                }
            }

            if (totalGaps > 0)
            {
                fragmentation = 1 - (usableGaps / totalGaps);
            }

            return fragmentation;
        }

        // Parçacığı tahsislere dönüştür
        private List<AllocatedBlock> ConvertParticleToAllocations(Particle particle, List<FrequencyAllocationRequest> requests)
        {
            var allocations = new List<AllocatedBlock>();

            for (int i = 0; i < particle.Position.Count; i++)
            {
                if (particle.ValidPositions[i])
                {
                    allocations.Add(new AllocatedBlock
                    {
                        CenterFrequency = particle.Position[i],
                        Bandwidth = requests[i].Bandwidth,
                        AllocationId = requests[i].RequestId ?? Guid.NewGuid().ToString(),
                        AllocationTime = DateTime.Now
                    });
                }
            }

            return allocations;
        }

        // Toplu optimizasyon skoru hesaplama
        private double CalculateBatchOptimizationScore(List<AllocatedBlock> allocations)
        {
            if (allocations.Count == 0) return 0;

            double score = 100;

            // Fragmentasyon azaltma bonusu
            double fragmentation = CalculateBatchFragmentation(allocations);
            score -= fragmentation * 30;

            // Spektrum verimliliği bonusu
            double utilization = allocations.Sum(a => a.Bandwidth) / (maxFreq - minFreq);
            score += utilization * 20;

            // Bitişik yerleşim bonusu
            var sorted = allocations.OrderBy(a => a.CenterFrequency).ToList();
            int contiguousCount = 0;
            for (int i = 0; i < sorted.Count - 1; i++)
            {
                if (Math.Abs(sorted[i].EndFreq - sorted[i + 1].StartFreq) < guardBand * 3)
                {
                    contiguousCount++;
                }
            }
            score += (contiguousCount / (double)Math.Max(1, sorted.Count - 1)) * 30;

            return Math.Max(0, Math.Min(100, score));
        }

        // Toplam fragmentasyon hesaplama
        private double CalculateTotalFragmentation()
        {
            return CalculateBatchFragmentation(new List<AllocatedBlock>());
        }

        // Ana tahsis fonksiyonu
        public AllocatedBlock AllocateFrequency(FrequencyAllocationRequest request)
        {
            if (request.Bandwidth <= 0 || request.Bandwidth > (maxFreq - minFreq))
                return null;

            // Genetik algoritma ile aday çözümler üret
            var candidates = GenerateCandidateSolutions(request.Bandwidth, 50);

            // Simulated Annealing ile optimizasyon
            var bestCandidate = OptimizeWithSimulatedAnnealing(candidates, request.Bandwidth);

            if (bestCandidate != null)
            {
                var allocation = new AllocatedBlock
                {
                    CenterFrequency = bestCandidate.Value,
                    Bandwidth = request.Bandwidth,
                    AllocationId = request.RequestId ?? Guid.NewGuid().ToString(),
                    AllocationTime = DateTime.Now
                };

                allocatedBlocks.Add(allocation);
                UpdateFragmentationHistory(allocation.CenterFrequency);

                return allocation;
            }

            return null;
        }

        // Genetik algoritma ile aday çözümler üretme
        private List<double> GenerateCandidateSolutions(double bandwidth, int populationSize)
        {
            var candidates = new List<double>();
            var availableGaps = FindAvailableGaps(bandwidth);

            if (availableGaps.Count == 0)
                return candidates;

            // Elitist seçim - en iyi boşlukları al
            foreach (var gap in availableGaps.Take(populationSize / 2))
            {
                // Boşluğun ortasına yerleştir
                double centerFreq = (gap.Item1 + gap.Item2) / 2;
                candidates.Add(centerFreq);

                // Rastgele mutasyon uygula
                if (random.NextDouble() < 0.3)
                {
                    double mutation = (random.NextDouble() - 0.5) * bandwidth * 0.2;
                    double mutated = centerFreq + mutation;
                    if (IsValidPosition(mutated, bandwidth))
                        candidates.Add(mutated);
                }
            }

            // Rastgele çözümler ekle (çeşitlilik için)
            int remaining = populationSize - candidates.Count;
            for (int i = 0; i < remaining; i++)
            {
                double randomFreq = minFreq + (maxFreq - minFreq) * random.NextDouble();
                if (IsValidPosition(randomFreq, bandwidth))
                    candidates.Add(randomFreq);
            }

            return candidates;
        }

        // Simulated Annealing optimizasyonu
        private double? OptimizeWithSimulatedAnnealing(List<double> candidates, double bandwidth)
        {
            if (candidates.Count == 0)
                return null;

            double currentBest = candidates[0];
            double currentCost = CalculateCost(currentBest, bandwidth);
            double temp = temperature;

            foreach (var candidate in candidates)
            {
                double candidateCost = CalculateCost(candidate, bandwidth);
                double delta = candidateCost - currentCost;

                // Daha iyi çözüm veya termal kabul
                if (delta < 0 /*|| random.NextDouble() < Math.Exp(-delta / temp)*/)
                {
                    currentBest = candidate;
                    currentCost = candidateCost;
                }

                temp *= coolingRate;
            }

            temperature = Math.Max(temperature * 0.99, 1.0); // Global sıcaklık azaltma
            return currentBest;
        }

        // Maliyet fonksiyonu (düşük = daha iyi)
        private double CalculateCost(double centerFreq, double bandwidth)
        {
            double cost = 0;

            // 1. Spektrum kenarlarına yakınlık cezası
            //double edgeDistance = Math.Min(centerFreq - bandwidth / 2 - minFreq,
            //                              maxFreq - (centerFreq + bandwidth / 2));
            //cost += Math.Exp(-edgeDistance * 10) * 100;

            // 2. Fragmentasyon cezası
            double fragmentation = CalculateFragmentation(centerFreq, bandwidth);
            cost += fragmentation * 50;

            // 3. Diğer tahsislere yakınlık (girişim) cezası
            foreach (var block in allocatedBlocks)
            {
                double distance = Math.Abs(centerFreq - block.CenterFrequency);
                if (distance < (bandwidth + block.Bandwidth) / 2 + guardBand * 5)
                {
                    cost += 1000 / (distance + 0.1);
                }
            }

            // 4. Geçmiş fragmentasyon deneyimi
            if (fragmentationHistory.ContainsKey(Math.Round(centerFreq, 2)))
            {
                cost += fragmentationHistory[Math.Round(centerFreq, 2)] * 10;
            }

            // 5. Spektrum verimliliği bonusu (ardışık tahsisleri teşvik)
            //double efficiency = CalculateSpectrumEfficiency(centerFreq, bandwidth);
            //cost -= efficiency * 30;

            return cost;
        }

        // Fragmentasyon hesaplama
        private double CalculateFragmentation(double centerFreq, double bandwidth)
        {
            double startFreq = centerFreq - bandwidth / 2;
            double endFreq = centerFreq + bandwidth / 2;

            var sortedBlocks = allocatedBlocks.OrderBy(b => b.CenterFrequency).ToList();
            double fragmentation = 0;

            foreach (var block in sortedBlocks)
            {
                // Bloklar arası küçük boşlukları say
                double gap = Math.Min(Math.Abs(startFreq - block.EndFreq),
                                     Math.Abs(endFreq - block.StartFreq));
                if (gap > 0 && gap < bandwidth * 0.5)
                {
                    fragmentation += 1.0 / (gap + 0.1);
                }
            }

            return fragmentation;
        }

        // Spektrum verimliliği hesaplama
        private double CalculateSpectrumEfficiency(double centerFreq, double bandwidth)
        {
            double efficiency = 0;
            double startFreq = centerFreq - bandwidth / 2;
            double endFreq = centerFreq + bandwidth / 2;

            foreach (var block in allocatedBlocks)
            {
                // Bitişik blokları tespit et
                if (Math.Abs(block.EndFreq - startFreq) < guardBand * 2 ||
                    Math.Abs(block.StartFreq - endFreq) < guardBand * 2)
                {
                    efficiency += 10;
                }
            }

            return efficiency;
        }

        // Boş alanları bul
        private List<Tuple<double, double>> FindAvailableGaps(double bandwidth)
        {
            var gaps = new List<Tuple<double, double>>();
            var sorted = allocatedBlocks.OrderBy(b => b.StartFreq).ToList();

            // İlk boşluk
            if (sorted.Count == 0 || sorted[0].StartFreq - minFreq >= bandwidth + guardBand)
            {
                gaps.Add(new Tuple<double, double>(minFreq, sorted.Count > 0 ? sorted[0].StartFreq - guardBand : maxFreq));
            }

            // Ara boşluklar
            for (int i = 0; i < sorted.Count - 1; i++)
            {
                double gapStart = sorted[i].EndFreq + guardBand;
                double gapEnd = sorted[i + 1].StartFreq - guardBand;

                if (gapEnd - gapStart >= bandwidth)
                {
                    gaps.Add(new Tuple<double, double>(gapStart, gapEnd));
                }
            }

            // Son boşluk
            if (sorted.Count > 0 && maxFreq - sorted.Last().EndFreq >= bandwidth + guardBand)
            {
                gaps.Add(new Tuple<double, double>(sorted.Last().EndFreq + guardBand, maxFreq));
            }

            return gaps.OrderByDescending(g => g.Item2 - g.Item1).ToList();
        }

        // Pozisyon geçerliliği kontrolü
        private bool IsValidPosition(double centerFreq, double bandwidth)
        {
            double startFreq = centerFreq - bandwidth / 2;
            double endFreq = centerFreq + bandwidth / 2;

            if (startFreq < minFreq || endFreq > maxFreq)
                return false;

            foreach (var block in allocatedBlocks)
            {
                if (!(endFreq + guardBand <= block.StartFreq || startFreq - guardBand >= block.EndFreq))
                    return false;
            }

            return true;
        }

        // Fragmentasyon geçmişini güncelle
        private void UpdateFragmentationHistory(double centerFreq)
        {
            double key = Math.Round(centerFreq, 2);
            double fragmentation = CalculateFragmentation(centerFreq, 0);

            if (fragmentationHistory.ContainsKey(key))
            {
                fragmentationHistory[key] = fragmentationHistory[key] * (1 - learningRate) +
                                           fragmentation * learningRate;
            }
            else
            {
                fragmentationHistory[key] = fragmentation;
            }
        }

        public List<AllocatedBlock> GetAllocatedBlocks() => allocatedBlocks;

        public void DeallocateFrequency(string allocationId)
        {
            allocatedBlocks.RemoveAll(b => b.AllocationId == allocationId);
        }

        public double GetSpectrumUtilization()
        {
            double totalAllocated = allocatedBlocks.Sum(b => b.Bandwidth);
            return totalAllocated / (maxFreq - minFreq) * 100;
        }
    }
}
