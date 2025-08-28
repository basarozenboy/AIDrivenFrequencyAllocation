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
