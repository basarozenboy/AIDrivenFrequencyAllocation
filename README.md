# AIDrivenFrequencyAllocation — Teknik Spesifikasyon

Bu doküman, projeyi **sıfırdan yeniden yazacak bir yapay zekâ aracına** verilmek üzere hazırlanmış eksiksiz bir teknik spesifikasyondur. Mevcut kod tabanındaki her sınıf, alan, sabit, formül ve arayüz davranışı burada birebir belirtilmiştir. Amaç: RF spektrumunda dinamik frekans tahsisi yapan, yapay zekâ/sezgisel algoritmalar kullanan bir sistem üretmek — **.NET 8 çekirdek kütüphane + ASP.NET Core Web API** arka uç, **React / Next.js (en güncel sürüm)** ile modern görünümlü bir web arayüzü. **Windows Forms KULLANILMAYACAK** — eski masaüstü arayüzü tamamen kaldırılıp yerine tarayıcı tabanlı, responsive ve modern bir SPA/SSR arayüz getirilecektir.

---

## 1. Genel Bakış

Sistem, bant genişliği (bandwidth) talepleri geldikçe, belirli bir frekans aralığı [minFreq, maxFreq] (GHz) içinde:

- Talep edilen bant genişliğini karşılayan,
- Diğer tahsislerle çakışmayan (aralarında sabit bir **guard band** bırakan),
- Spektrumu mümkün olduğunca az parçalayan (fragmentasyon) ve/veya tahsisleri birbirinden mümkün olduğunca uzağa yerleştiren

merkez frekansları hesaplar. Hem **tekil tahsis** (bir seferde bir istek) hem de **toplu tahsis** (bir seferde N istek) desteklenir. Tekil tahsiste genetik algoritma + simulated annealing tarzı bir maliyet minimizasyonu, toplu tahsiste ise boşluk tabanlı yerleştirme + iteratif "maksimum uzaklık" optimizasyonu kullanılır. Ayrıca kod içinde kullanılmayan (dead code / alternatif yol) bir **Particle Swarm Optimization (PSO)** implementasyonu da bulunur; bu bölüm 8'de ayrıca belirtilmiştir.

Tüm frekans değerleri **GHz**, bant genişlikleri motor içinde **GHz**, ancak arayüzde kullanıcıya **MHz** olarak gösterilir/girilir (dönüşüm: `GHz = MHz / 1000.0`).

### Mimari (yeni)

```
┌─────────────────────────┐      HTTP/JSON      ┌──────────────────────────────┐
│  Next.js Web Arayüzü     │  ───────────────▶   │  ASP.NET Core Web API        │
│  (React, TypeScript,     │  ◀───────────────   │  (AIDrivenFrequencyAllocation │
│   Tailwind CSS, modern)  │                      │   .Api)                      │
└─────────────────────────┘                      └──────────────┬───────────────┘
                                                                   │ referans
                                                                   ▼
                                                   ┌──────────────────────────────┐
                                                   │ AIDrivenFrequencyAllocation   │
                                                   │ (çekirdek algoritma kütüphanesi│
                                                   │  — bölüm 3-8, DEĞİŞMEDİ)      │
                                                   └──────────────────────────────┘
```

Tahsis algoritmalarının kendisi (bölüm 3-8) **teknoloji/platform bağımsızdır** ve olduğu gibi korunur; değişen tek şey sunum katmanıdır: eski Windows Forms masaüstü uygulaması yerine bir Web API + Next.js arayüzü.

---

## 2. Çözüm ve Proje Yapısı

```
AIDrivenFrequencyAllocation.sln
AIDrivenFrequencyAllocation/              (çekirdek kütüphane — bkz. bölüm 3-8, mantık DEĞİŞMEDİ)
  AIDrivenFrequencyAllocation.csproj
  IntelligentFrequencyAllocator.cs
  FrequencyAllocationRequest.cs
  AllocatedBlock.cs
  BatchAllocationResult.cs
  Program.cs
AIDrivenFrequencyAllocation.Api/          (YENİ — ASP.NET Core Minimal API, eski FreqAllocationUI'nin yerini alır)
  AIDrivenFrequencyAllocation.Api.csproj
  Program.cs
  AllocationSessionService.cs
  Dtos.cs
web/                                       (YENİ — Next.js uygulaması; .sln'e dahil DEĞİL, repo kökünde ayrı bir Node.js projesi)
  package.json
  next.config.ts
  tsconfig.json
  app/
    layout.tsx
    page.tsx
    globals.css
  components/
    RangeSetupScreen.tsx
    SpectrumVisualizer.tsx
    AllocationControlPanel.tsx
    AllocationList.tsx
    BatchAllocationDialog.tsx
    UtilizationGauge.tsx
  lib/
    api.ts
    types.ts
```

> **Not:** `FreqAllocationUI` (Windows Forms) projesi tamamen kaldırılmıştır. Onun yerini bir ASP.NET Core Web API projesi (`AIDrivenFrequencyAllocation.Api`) ve ayrı bir Next.js web uygulaması (`web/`) alır. Backend artık **Windows'a bağımlı değildir** (eski `net8.0-windows` + WinForms hedefi kaldırıldı), cross-platform çalışır.

### 2.1 `AIDrivenFrequencyAllocation.csproj` (çekirdek kütüphane — değişmedi)

```xml
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <OutputType>Exe</OutputType>
    <TargetFramework>net8.0</TargetFramework>
    <ImplicitUsings>enable</ImplicitUsings>
    <Nullable>enable</Nullable>
  </PropertyGroup>
</Project>
```

Not: Bu proje `OutputType=Exe` olsa da `Program.cs` içeriği sadece `Console.WriteLine("Hello, World!");` — gerçek bir demo/test girişi yoktur, motor tamamen `IntelligentFrequencyAllocator` sınıfı üzerinden `AIDrivenFrequencyAllocation.Api` projesi tarafından tüketilir.

### 2.2 `AIDrivenFrequencyAllocation.Api.csproj` (YENİ)

```xml
<Project Sdk="Microsoft.NET.Sdk.Web">
  <PropertyGroup>
    <TargetFramework>net8.0</TargetFramework>
    <Nullable>enable</Nullable>
    <ImplicitUsings>enable</ImplicitUsings>
    <InvariantGlobalization>true</InvariantGlobalization>
  </PropertyGroup>
  <ItemGroup>
    <ProjectReference Include="..\AIDrivenFrequencyAllocation\AIDrivenFrequencyAllocation.csproj" />
  </ItemGroup>
</Project>
```

Windows'a özgü hiçbir bağımlılık yoktur (`Sdk.Web`, `net8.0` — `net8.0-windows` değil).

### 2.3 `web/package.json` (YENİ, özet)

En güncel kararlı sürümler kullanılmalıdır (yeniden yazım anında `npm create next-app@latest` ile üretilip güncellenmelidir). Beklenen asgari yığın:

- **Next.js** — en güncel sürüm, **App Router** kullanılmalı (Pages Router değil)
- **React** — Next.js'in desteklediği en güncel sürüm (Next.js kurulumunun getirdiği sürüm esas alınır)
- **TypeScript** — strict mode açık
- **Tailwind CSS** — en güncel sürüm (modern görünüm için birincil stil aracı)
- **shadcn/ui** (Radix UI tabanlı, Tailwind ile) — buton, dialog/modal, input, card, toast/sonner, progress bar gibi erişilebilir ve modern hazır bileşenler için önerilir
- **lucide-react** — ikon seti (eski emoji göstergelerinin yerini modern ikonlar alır)
- **sonner** (veya shadcn'in kendi toast bileşeni) — bildirimler için

```jsonc
{
  "name": "web",
  "private": true,
  "scripts": {
    "dev": "next dev",
    "build": "next build",
    "start": "next start",
    "lint": "next lint"
  },
  "dependencies": {
    "next": "latest",
    "react": "latest",
    "react-dom": "latest",
    "lucide-react": "latest",
    "sonner": "latest"
  },
  "devDependencies": {
    "typescript": "latest",
    "tailwindcss": "latest",
    "@types/node": "latest",
    "@types/react": "latest",
    "@types/react-dom": "latest"
  }
}
```

---

## 3. Veri Modelleri (`AIDrivenFrequencyAllocation` namespace) — DEĞİŞMEDİ

### 3.1 `FrequencyAllocationRequest`

```csharp
public class FrequencyAllocationRequest
{
    public double Bandwidth { get; set; }      // GHz cinsinden (API'de MHz alınıp /1000 ile dönüştürülür)
    public string RequestId { get; set; }
    public DateTime RequestTime { get; set; }
    public int Priority { get; set; } = 1;      // Şu an algoritmalarda kullanılmıyor, ileride öncelik sıralaması için ayrılmış
}
```

### 3.2 `AllocatedBlock`

```csharp
public class AllocatedBlock
{
    public double CenterFrequency { get; set; }
    public double Bandwidth { get; set; }
    public double StartFreq => CenterFrequency - Bandwidth / 2;   // hesaplanan (computed) özellik
    public double EndFreq   => CenterFrequency + Bandwidth / 2;   // hesaplanan (computed) özellik
    public string AllocationId { get; set; }
    public DateTime AllocationTime { get; set; }
}
```

### 3.3 `BatchAllocationResult`

```csharp
public class BatchAllocationResult
{
    public List<AllocatedBlock> SuccessfulAllocations { get; set; }
    public List<FrequencyAllocationRequest> FailedRequests { get; set; }
    public double TotalFragmentation { get; set; }   // ortalama boşluk büyüklüğü (GHz), bkz. 6.7
    public double OptimizationScore { get; set; }    // 0-100 arası, bkz. 6.8
}
```

---

## 4. `IntelligentFrequencyAllocator` — Alanlar ve Kurucu

```csharp
public class IntelligentFrequencyAllocator
{
    private double minFreq;                                  // GHz, kurucudan gelir
    private double maxFreq;                                  // GHz, kurucudan gelir
    private List<AllocatedBlock> allocatedBlocks;            // kalıcı tahsis listesi
    private Random random;
    private double guardBand = 0.01;                         // 10 MHz guard band (GHz cinsinden)

    // ML parametreleri
    private double learningRate = 0.1;
    private Dictionary<double, double> fragmentationHistory; // key: Math.Round(centerFreq, 2)
    private double temperature = 100.0;                      // simulated annealing sıcaklığı, tahsisler arasında kalıcı
    private double coolingRate = 0.95;

    public IntelligentFrequencyAllocator(double minFreqGHz, double maxFreqGHz)
    {
        minFreq = minFreqGHz;
        maxFreq = maxFreqGHz;
        allocatedBlocks = new List<AllocatedBlock>();
        random = new Random();
        fragmentationHistory = new Dictionary<double, double>();
    }

    // YENİ — eski Windows Forms UI'nin reflection ile private alan okumasının (bkz. eski kusur listesi, bölüm 10-B.7)
    // yerini alan public salt-okunur erişim. Davranışı DEĞİŞTİRMEZ, sadece API/servis katmanının
    // aralığı sağlam biçimde okumasını sağlar.
    public double MinFrequency => minFreq;
    public double MaxFrequency => maxFreq;
}
```

Önemli: `temperature`, her `AllocateFrequency` çağrısından sonra `temperature = Math.Max(temperature * 0.99, 1.0)` ile küçültülür ve bir sonraki çağrıya taşınır (instance seviyesinde kalıcıdır, her çağrıda sıfırlanmaz).

### Public API yüzeyi

```csharp
public double               MinFrequency { get; }   // YENİ
public double               MaxFrequency { get; }   // YENİ
public AllocatedBlock       AllocateFrequency(FrequencyAllocationRequest request);
public BatchAllocationResult AllocateBatch(List<FrequencyAllocationRequest> requests);
public List<AllocatedBlock> GetAllocatedBlocks();
public void                 DeallocateFrequency(string allocationId);
public double               GetSpectrumUtilization();
```

---

## 5. Tekil Tahsis: `AllocateFrequency(request)`

```
1. request.Bandwidth <= 0 veya > (maxFreq - minFreq) ise → null döndür.
2. candidates = GenerateCandidateSolutions(bandwidth, populationSize: 50)
3. bestCandidate = OptimizeWithSimulatedAnnealing(candidates, bandwidth)
4. bestCandidate null ise → null döndür.
5. Aksi halde:
     allocation = new AllocatedBlock {
        CenterFrequency = bestCandidate.Value,
        Bandwidth = request.Bandwidth,
        AllocationId = request.RequestId ?? Guid.NewGuid().ToString(),
        AllocationTime = DateTime.Now
     }
     allocatedBlocks.Add(allocation)
     UpdateFragmentationHistory(allocation.CenterFrequency)
     return allocation
```

### 5.1 `GenerateCandidateSolutions(bandwidth, populationSize)` — "genetik algoritma" aday üretimi

```
availableGaps = FindAvailableGaps(bandwidth)   // bkz. 5.3, büyükten küçüğe sıralı
if availableGaps boşsa → boş liste döndür

// Elitist seçim: en geniş boşlukların (populationSize/2 adet) ortasına aday koy
for gap in availableGaps.Take(populationSize / 2):
    centerFreq = (gap.Start + gap.End) / 2
    candidates.Add(centerFreq)

    // %30 olasılıkla mutasyon: ±(bandwidth * 0.2 / 2) aralığında rastgele kaydırma
    if random.NextDouble() < 0.3:
        mutation = (random.NextDouble() - 0.5) * bandwidth * 0.2
        mutated = centerFreq + mutation
        if IsValidPosition(mutated, bandwidth): candidates.Add(mutated)

// Çeşitlilik için kalan popülasyonu tamamen rastgele doldur
remaining = populationSize - candidates.Count
repeat remaining kez:
    randomFreq = minFreq + (maxFreq - minFreq) * random.NextDouble()
    if IsValidPosition(randomFreq, bandwidth): candidates.Add(randomFreq)

return candidates
```

### 5.2 `OptimizeWithSimulatedAnnealing(candidates, bandwidth)`

```
if candidates boşsa → null

currentBest = candidates[0]
currentCost = CalculateCost(currentBest, bandwidth)
temp = temperature   // instance alanından başlar

for candidate in candidates (liste sırasıyla, TEK geçiş):
    candidateCost = CalculateCost(candidate, bandwidth)
    delta = candidateCost - currentCost
    if delta < 0:                     // NOT: olasılıksal kabul satırı kodda YORUM SATIRI, aktif değil
        currentBest = candidate
        currentCost = candidateCost
    temp *= coolingRate                // 0.95

temperature = max(temperature * 0.99, 1.0)   // global/instance sıcaklık kalıcı olarak azalır
return currentBest
```

> Önemli davranış notu: Kodda `delta < 0 /*|| random.NextDouble() < Math.Exp(-delta / temp)*/` satırı var — yani algoritma isim olarak "simulated annealing" olsa da, **kötü çözümleri termal olarak kabul etme mekanizması devre dışı bırakılmış**; fiilen sadece "aday listesindeki ilk elemandan başlayarak greedy şekilde en düşük maliyetliyi seç" davranışını uygular. Bu, bölüm 10-A'da "korunması gereken kusur" olarak işaretlenmiştir; **birebir bu haliyle** (aktif olmayan olasılıksal kabul dahil) korunmalıdır.

### 5.3 `FindAvailableGaps(bandwidth)` — kalıcı `allocatedBlocks` üzerinde

```
sorted = allocatedBlocks, StartFreq'e göre artan sıralı

// İlk boşluk (spektrum başlangıcı ile ilk blok arası)
if sorted boşsa OR sorted[0].StartFreq - minFreq >= bandwidth + guardBand:
    gaps.Add( [minFreq, sorted boşsa maxFreq değilse sorted[0].StartFreq - guardBand] )

// Ara boşluklar
for i in 0..sorted.Count-2:
    gapStart = sorted[i].EndFreq + guardBand
    gapEnd   = sorted[i+1].StartFreq - guardBand
    if (gapEnd - gapStart) >= bandwidth:
        gaps.Add([gapStart, gapEnd])

// Son boşluk (son blok ile spektrum sonu arası)
if sorted doluysa AND maxFreq - sorted.Last().EndFreq >= bandwidth + guardBand:
    gaps.Add([sorted.Last().EndFreq + guardBand, maxFreq])

return gaps, (End - Start) büyüklüğüne göre AZALAN sıralı
```

### 5.4 `IsValidPosition(centerFreq, bandwidth)`

```
startFreq = centerFreq - bandwidth/2
endFreq   = centerFreq + bandwidth/2
if startFreq < minFreq or endFreq > maxFreq: return false
for block in allocatedBlocks:
    // çakışma yok mu kontrolü (guard band dahil)
    if NOT (endFreq + guardBand <= block.StartFreq OR startFreq - guardBand >= block.EndFreq):
        return false
return true
```

### 5.5 `CalculateCost(centerFreq, bandwidth)` — düşük = daha iyi

```
cost = 0

// (1) Spektrum kenarına yakınlık cezası — KODDA TAMAMEN YORUM SATIRI, AKTİF DEĞİL, dahil edilmiyor

// (2) Fragmentasyon cezası
cost += CalculateFragmentation(centerFreq, bandwidth) * 50

// (3) Diğer tahsislere yakınlık / girişim cezası
for block in allocatedBlocks:
    distance = |centerFreq - block.CenterFrequency|
    if distance < (bandwidth + block.Bandwidth)/2 + guardBand * 5:
        cost += 1000 / (distance + 0.1)

// (4) Geçmiş fragmentasyon deneyimi (öğrenilmiş ceza)
key = Math.Round(centerFreq, 2)
if fragmentationHistory.ContainsKey(key):
    cost += fragmentationHistory[key] * 10

// (5) Spektrum verimliliği bonusu — KODDA YORUM SATIRI, AKTİF DEĞİL

return cost
```

### 5.6 `CalculateFragmentation(centerFreq, bandwidth)`

```
startFreq = centerFreq - bandwidth/2
endFreq   = centerFreq + bandwidth/2
fragmentation = 0
for block in allocatedBlocks (CenterFrequency'e göre sıralı — sıralamanın sonuca etkisi yok, sadece toplanıyor):
    gap = min(|startFreq - block.EndFreq|, |endFreq - block.StartFreq|)
    if gap > 0 and gap < bandwidth * 0.5:
        fragmentation += 1.0 / (gap + 0.1)
return fragmentation
```

### 5.7 `UpdateFragmentationHistory(centerFreq)`

```
key = Math.Round(centerFreq, 2)
fragmentation = CalculateFragmentation(centerFreq, bandwidth: 0)   // NOT: bandwidth=0 ile çağrılıyor
if key zaten var ise:
    fragmentationHistory[key] = eski * (1 - learningRate) + fragmentation * learningRate   // learningRate=0.1, üstel hareketli ortalama
else:
    fragmentationHistory[key] = fragmentation
```

### 5.8 `CalculateSpectrumEfficiency(centerFreq, bandwidth)` — tanımlı ama hiçbir yerden çağrılmıyor (dead code)

```
efficiency = 0
startFreq = centerFreq - bandwidth/2
endFreq   = centerFreq + bandwidth/2
for block in allocatedBlocks:
    if |block.EndFreq - startFreq| < guardBand*2 OR |block.StartFreq - endFreq| < guardBand*2:
        efficiency += 10
return efficiency
```

Yeniden yazımda bu metod tanımlı tutulmalı (kullanılmasa da), orijinal davranışla birebir eşleşmesi için.

---

## 6. Toplu Tahsis: `AllocateBatch(requests)`

Bu, **aktif olarak çağrılan** toplu tahsis yoludur (PSO tabanlı `FindOptimalBatchAllocation` değil — bkz. bölüm 8).

```
result = new BatchAllocationResult { SuccessfulAllocations=[], FailedRequests=[], TotalFragmentation=0, OptimizationScore=0 }
if requests null veya boşsa → result döndür (boş haliyle)

// 1) Geçersiz istekleri ele (Bandwidth <= 0 veya > (maxFreq-minFreq))
validRequests = requests.Where(geçerli), geçersizler result.FailedRequests'e eklenir
if validRequests boşsa → result döndür

// 2) Büyükten küçüğe sırala (büyük bantlar önce yerleştirilir)
validRequests = validRequests.OrderByDescending(Bandwidth)

// 3) Geçici (henüz kalıcı olmayan) tahsis simülasyonu
tempAllocations = []
tempAllocatedBlocks = allocatedBlocks kopyası (yeni List)

for request in validRequests:
    position = FindOptimalBatchPosition(request.Bandwidth, tempAllocatedBlocks)   // bkz. 6.1
    if position varsa:
        allocation = new AllocatedBlock {
            CenterFrequency = position.Value,
            Bandwidth = request.Bandwidth,
            AllocationId = request.RequestId ?? Guid.NewGuid().ToString(),
            AllocationTime = DateTime.Now
        }
        tempAllocations.Add(allocation)
        tempAllocatedBlocks.Add(allocation)
    else:
        result.FailedRequests.Add(request)

// 4) Tüm yeni tahsisleri optimize et (maksimum uzaklık için, iteratif)
optimizedAllocations = OptimizeBatchDistribution(tempAllocations, tempAllocatedBlocks)   // bkz. 6.4

// 5) Kalıcı listeye ekle
allocatedBlocks.AddRange(optimizedAllocations)
result.SuccessfulAllocations = optimizedAllocations

// 6) Metrikleri hesapla (kalıcı listeye eklendikten SONRA, yani TÜM tahsisler üzerinden)
result.TotalFragmentation = CalculateTotalFragmentationForBatch()   // bkz. 6.7
result.OptimizationScore  = CalculateOptimizationScore(optimizedAllocations)  // bkz. 6.8, sadece yeni eklenenler parametre ama içeride tüm allocatedBlocks kullanılıyor

return result
```

### 6.1 `FindOptimalBatchPosition(bandwidth, currentBlocks)`

```
gaps = FindAvailableGapsWithBlocks(bandwidth, currentBlocks)   // bkz. 6.2
if gaps boşsa → null
largestGap = gaps, (End-Start) en büyük olan
optimalPosition = CalculateMaxDistancePointWithBlocks(largestGap.Start, largestGap.End, bandwidth, currentBlocks)
return optimalPosition
```

### 6.2 `FindAvailableGapsWithBlocks(bandwidth, blocks)` — `FindAvailableGaps`'e benzer ama parametre olarak verilen blok listesini kullanır ve GUARD BAND HESABI FARKLIDIR

```
sorted = blocks, StartFreq'e göre artan

if sorted boşsa: gaps.Add([minFreq, maxFreq]); return

// İlk boşluk — DİKKAT: burada eşik "bandwidth + guardBand*2" (5.3'teki tekil sürümde guardBand*1 idi)
if sorted[0].StartFreq - minFreq >= bandwidth + guardBand*2:
    gaps.Add([minFreq, sorted[0].StartFreq - guardBand])

for i in 0..sorted.Count-2:
    gapStart = sorted[i].EndFreq + guardBand
    gapEnd   = sorted[i+1].StartFreq - guardBand
    if (gapEnd - gapStart) >= bandwidth:
        gaps.Add([gapStart, gapEnd])

// Son boşluk — yine eşik guardBand*2
if sorted doluysa AND maxFreq - sorted.Last().EndFreq >= bandwidth + guardBand*2:
    gaps.Add([sorted.Last().EndFreq + guardBand, maxFreq])

return gaps   // NOT: burada büyükten küçüğe SIRALANMIYOR (5.3'ten farklı), çağıran taraf kendisi OrderByDescending yapıyor
```

Bu iki benzer-ama-farklı gap-bulma fonksiyonunun (5.3 ve 6.2) **birebir bu haliyle** (guard band çarpanı farkı dahil) yeniden yazılması, davranışsal eşdeğerlik için önemlidir.

### 6.3 `CalculateMaxDistancePointWithBlocks(gapStart, gapEnd, bandwidth, blocks)`

```
halfBandwidth = bandwidth / 2
searchStart = gapStart + halfBandwidth
searchEnd   = gapEnd   - halfBandwidth

if searchEnd <= searchStart: return (gapStart+gapEnd)/2
if blocks.Count <= 1: return (gapStart+gapEnd)/2

sampleCount = 100
step = (searchEnd - searchStart) / sampleCount

bestPosition = searchStart
maxMinDistance = 0
for i in 0..sampleCount (dahil, yani 101 örnek):
    candidatePos = searchStart + i*step
    minDistance = min over blocks of |candidatePos - block.CenterFrequency|
    if minDistance > maxMinDistance:
        maxMinDistance = minDistance
        bestPosition = candidatePos

return bestPosition
```

Bu, gap içinde **tüm mevcut bloklara olan en yakın mesafeyi maksimize eden** noktayı 101 nokta örnekleyerek (brute-force grid search) bulur.

### 6.4 `OptimizeBatchDistribution(newAllocations, allBlocks)`

```
if newAllocations.Count <= 1: return newAllocations

maxIterations = 3
optimized = newAllocations kopyası

for iteration in 0..maxIterations-1:
    improved = false
    for i in 0..optimized.Count-1:
        current = optimized[i]
        otherBlocks = allBlocks.Where(AllocationId != current.AllocationId)
        betterPosition = FindBetterPositionForBlock(current, otherBlocks)   // bkz. 6.5
        if betterPosition var VE |betterPosition - current.CenterFrequency| > 0.01:
            optimized[i] = current'in bandwidth/id/time'ı aynı, CenterFrequency=betterPosition olan kopyası
            allBlocks içinde aynı AllocationId'ye sahip elemanı da güncelle (in-place)
            improved = true
    if not improved: break

return optimized
```

### 6.5 `FindBetterPositionForBlock(block, otherBlocks)`

```
gaps = FindAvailableGapsWithBlocks(block.Bandwidth, otherBlocks)
if gaps boşsa → null

currentMinDistance = CalculateMinDistanceToOthers(block.CenterFrequency, otherBlocks)   // bkz 6.6
bestPosition = block.CenterFrequency
bestMinDistance = currentMinDistance

for gap in gaps:
    candidatePos = CalculateMaxDistancePointWithBlocks(gap.Start, gap.End, block.Bandwidth, otherBlocks)
    candidateMinDistance = CalculateMinDistanceToOthers(candidatePos, otherBlocks)
    if candidateMinDistance > bestMinDistance:
        bestMinDistance = candidateMinDistance
        bestPosition = candidatePos

return |bestPosition - block.CenterFrequency| > 0.01 ? bestPosition : null
```

### 6.6 `CalculateMinDistanceToOthers(position, otherBlocks)`

```
if otherBlocks boşsa: return double.MaxValue
return min over otherBlocks of |position - block.CenterFrequency|
```

### 6.7 `CalculateTotalFragmentationForBatch()` — kalıcı `allocatedBlocks` üzerinden, ORTALAMA boşluk büyüklüğü

```
if allocatedBlocks.Count < 2: return 0
sorted = allocatedBlocks, StartFreq'e göre artan
totalFragmentation = 0; fragmentCount = 0

if sorted[0].StartFreq - minFreq > guardBand:
    totalFragmentation += (sorted[0].StartFreq - minFreq); fragmentCount++

for i in 0..sorted.Count-2:
    gapSize = sorted[i+1].StartFreq - sorted[i].EndFreq
    if gapSize > guardBand: totalFragmentation += gapSize; fragmentCount++

if maxFreq - sorted.Last().EndFreq > guardBand:
    totalFragmentation += (maxFreq - sorted.Last().EndFreq); fragmentCount++

return fragmentCount > 0 ? totalFragmentation / fragmentCount : 0
```

Bu değer, GHz cinsinden **ortalama boşluk büyüklüğüdür**. API bunu olduğu gibi (GHz) döner; arayüz katmanında nasıl gösterileceği bölüm 9.3'te (fragmentasyon kartı) belirtilmiştir — eski WinForms'daki yanıltıcı `:P1` (yüzde) formatı **kullanılmayacaktır** (bkz. bölüm 10-B.1).

### 6.8 `CalculateOptimizationScore(newAllocations)` — 0-100 arası, kalıcı `allocatedBlocks`'a bakarak hesaplanır

```
if newAllocations boşsa: return 0
score = 100.0

// (1) Minimum mesafe skoru (0-40 puan ceza payı) — sadece allocatedBlocks.Count >= 2 ise
if allocatedBlocks.Count >= 2:
    sorted = allocatedBlocks, CenterFrequency'e göre artan
    minDistance = ardışık merkezler arası en küçük fark
    idealMinDistance = (maxFreq - minFreq) / allocatedBlocks.Count
    distanceRatio = min(minDistance / idealMinDistance, 1.0)
    score -= (1.0 - distanceRatio) * 40

// (2) Spektrum kullanım verimliliği (ceza payı, GetSpectrumUtilization %'sine göre)
utilization = GetSpectrumUtilization()
if utilization > 90: score -= 20
else if utilization < 10: score -= 10

// (3) Fragmentasyon skoru
avgFragmentation = CalculateTotalFragmentation()      // bkz. not aşağıda
totalSpectrum = maxFreq - minFreq
fragmentationRatio = avgFragmentation / totalSpectrum
if fragmentationRatio > 0.1: score -= fragmentationRatio * 100

// (4) Uniform dağılım bonusu/cezası (0-10 puan)
uniformity = CalculateUniformityScore()               // bkz. 6.9
score -= (1.0 - uniformity) * 10

return clamp(score, 0, 100)
```

> **Bilinen kusur (birebir korunmalı):** `CalculateTotalFragmentation()` şu şekilde tanımlı: `return CalculateBatchFragmentation(new List<AllocatedBlock>());` — yani her zaman **boş bir liste** ile `CalculateBatchFragmentation`'ı çağırır. `CalculateBatchFragmentation`, kendi içinde `allocatedBlocks.Concat(newBlocks)` yaptığından, boş liste verilse de kalıcı `allocatedBlocks` üzerinden gerçek bir fragmentasyon hesaplar (bkz. 6.10) — yani fonksiyon ismi yanıltıcı olsa da **çalışıyor**. Yeniden yazımda `CalculateTotalFragmentation()` metodunun gövdesi birebir bu şekilde (`CalculateBatchFragmentation` içine boş liste geçerek) yazılmalıdır.

### 6.9 `CalculateUniformityScore()` — 0 (düzensiz) ile 1 (mükemmel uniform) arası

```
if allocatedBlocks.Count < 3: return 1.0
sorted = allocatedBlocks, CenterFrequency'e göre artan
distances = ardışık merkezler arası farklar listesi
if distances boşsa: return 1.0
mean = distances.Average()
variance = distances.Sum((d-mean)^2) / distances.Count
stdDev = sqrt(variance)
cv = mean > 0 ? stdDev/mean : 0     // varyasyon katsayısı
return exp(-cv)
```

### 6.10 `CalculateBatchFragmentation(newBlocks)`

```
allBlocks = allocatedBlocks.Concat(newBlocks), StartFreq'e göre artan
fragmentation = 0; totalGaps = 0; usableGaps = 0

for i in 0..allBlocks.Count-2:
    gap = allBlocks[i+1].StartFreq - allBlocks[i].EndFreq - guardBand*2
    if gap > 0:
        totalGaps += gap
        if gap >= 0.01: usableGaps += gap

if totalGaps > 0: fragmentation = 1 - (usableGaps/totalGaps)
return fragmentation
```

NOT: Bu fonksiyon spektrumun **başı ve sonu** ile ilk/son blok arasındaki boşlukları hesaba katmaz — yalnızca bloklar ARASI boşluklara bakar (6.7'deki `CalculateTotalFragmentationForBatch`'ten farklı olarak).

### 6.11 `CalculateBatchOptimizationScore(allocations)` — tanımlı ama hiçbir yerden çağrılmıyor (dead code, `AllocateBatch` akışında kullanılan asıl skor fonksiyonu 6.8'dir)

```
if allocations boşsa: return 0
score = 100
fragmentation = CalculateBatchFragmentation(allocations)
score -= fragmentation * 30

utilization = allocations.Sum(Bandwidth) / (maxFreq - minFreq)
score += utilization * 20

sorted = allocations, CenterFrequency'e göre artan
contiguousCount = ardışık ikili arasında |EndFreq_i - StartFreq_{i+1}| < guardBand*3 olan çift sayısı
score += (contiguousCount / max(1, sorted.Count-1)) * 30

return clamp(score, 0, 100)
```

Yeniden yazımda bu metod da (kullanılmasa da) tanımlı tutulmalıdır.

---

## 7. Tahsis Kaldırma ve Yardımcı Sorgular

```csharp
public List<AllocatedBlock> GetAllocatedBlocks() => allocatedBlocks;

public void DeallocateFrequency(string allocationId) =>
    allocatedBlocks.RemoveAll(b => b.AllocationId == allocationId);

public double GetSpectrumUtilization() =>
    allocatedBlocks.Sum(b => b.Bandwidth) / (maxFreq - minFreq) * 100;
```

---

## 8. Kullanılmayan Alternatif Yol: Particle Swarm Optimization (PSO)

`AllocateBatch` içinde ÇAĞRILMAYAN, ama sınıfta tam olarak implemente edilmiş bir toplu tahsis alternatifi vardır: `FindOptimalBatchAllocation(requests)`. Yeniden yazımda bu kod **birebir tanımlı tutulmalı** (dead code olarak, hiçbir public/aktif akıştan çağrılmadan) çünkü orijinal kod tabanının parçasıdır.

```
private class Particle {
    List<double> Position          // her istek için aday merkez frekans
    List<double> Velocity
    List<double> PersonalBest
    double Fitness
    double PersonalBestFitness
    List<bool> ValidPositions
    Particle Clone()               // derin kopya (listeler .ToList())
}

FindOptimalBatchAllocation(requests):
    swarmSize = 30
    maxIterations = 100
    particles = swarmSize adet CreateRandomParticle(requests)
    globalBest = particles içinde Fitness'i en düşük olan (düşük = iyi)

    for iter in 0..maxIterations-1:
        for particle in particles:
            UpdateParticleVelocity(particle, globalBest)
            UpdateParticlePosition(particle, requests)
            particle.Fitness = CalculateParticleFitness(particle)
            if particle.Fitness < particle.PersonalBestFitness:
                particle.PersonalBest = particle.Position kopyası
                particle.PersonalBestFitness = particle.Fitness
        currentBest = particles içinde en düşük Fitness
        if currentBest.Fitness < globalBest.Fitness:
            globalBest = currentBest.Clone()
        if globalBest.Fitness < 0.1: break   // erken durdurma

    return ConvertParticleToAllocations(globalBest, requests)

CreateRandomParticle(requests):
    tempAllocations = []
    for request in requests:
        validRange = FindValidRangeForRequest(request.Bandwidth, tempAllocations)
        if validRange var:
            centerFreq = validRange.Start + (validRange.End - validRange.Start) * random.NextDouble()
            Position.Add(centerFreq); ValidPositions.Add(true)
            tempAllocations.Add(AllocatedBlock{CenterFrequency=centerFreq, Bandwidth=request.Bandwidth, AllocationId=request.RequestId})
        else:
            Position.Add(minFreq + (maxFreq-minFreq)*random.NextDouble()); ValidPositions.Add(false)
        Velocity.Add((random.NextDouble()-0.5) * 0.1)
    PersonalBest = Position kopyası
    Fitness = CalculateParticleFitness(particle); PersonalBestFitness = Fitness

FindValidRangeForRequest(bandwidth, tempAllocations):
    allBlocks = allocatedBlocks ∪ tempAllocations, StartFreq'e göre artan
    // ilk boşluk / ara boşluklar (yalnızca İLK uygun ara boşluk) / son boşluk — merkez frekans için
    // (StartFreq/EndFreq yerine merkez frekans sınırları: start+bandwidth/2 ... end-bandwidth/2 mantığıyla)
    // bkz. orijinal kod bölüm için IntelligentFrequencyAllocator.cs satır ~540-575
    return bulunan aralık veya null

UpdateParticleVelocity(particle, globalBest):
    w=0.7 (atalet), c1=1.5 (kişisel), c2=1.5 (sosyal)
    for i in Velocity:
        r1,r2 = random.NextDouble() ikişer kez
        Velocity[i] = w*Velocity[i] + c1*r1*(PersonalBest[i]-Position[i]) + c2*r2*(globalBest.Position[i]-Position[i])
        Velocity[i] = clamp(Velocity[i], -0.5, 0.5)

UpdateParticlePosition(particle, requests):
    tempAllocations = []
    for i in Position:
        Position[i] += Velocity[i]
        halfBand = requests[i].Bandwidth/2
        Position[i] = clamp(Position[i], minFreq+halfBand, maxFreq-halfBand)
        block = AllocatedBlock{CenterFrequency=Position[i], Bandwidth=requests[i].Bandwidth, AllocationId=requests[i].RequestId}
        ValidPositions[i] = IsValidAllocation(block, tempAllocations)
        if ValidPositions[i]: tempAllocations.Add(block)

IsValidAllocation(newBlock, tempAllocations):
    allBlocks = allocatedBlocks ∪ tempAllocations
    for block in allBlocks:
        if çakışıyorsa (guardBand dahil, 5.4'teki mantıkla aynı): return false
    return newBlock sınırlar içinde (minFreq/maxFreq)

CalculateParticleFitness(particle) — düşük=iyi:
    fitness = 0
    validCount = ValidPositions içinde true sayısı
    fitness += (ValidPositions.Count - validCount) * 1000      // geçersiz pozisyon başına ağır ceza
    if validCount == 0: return fitness
    tempBlocks = geçerli pozisyonlar için AllocatedBlock listesi (Bandwidth SABİT 0.02 GHz kullanılıyor — gerçek bandwidth DEĞİL!)
    fitness += CalculateBatchFragmentation(tempBlocks) * 100
    tempBlocks CenterFrequency'e göre sıralanır
    for ardışık çift:
        gap = StartFreq_{i+1} - EndFreq_i
        if gap > guardBand*2 and gap < 0.05: fitness += gap*500   // küçük boşluk cezası
    centerPoint = (minFreq+maxFreq)/2
    deviation = tempBlocks.Average(|CenterFrequency - centerPoint|)
    fitness += deviation * 10
    return fitness

ConvertParticleToAllocations(particle, requests):
    geçerli (ValidPositions[i]==true) her i için AllocatedBlock{CenterFrequency=Position[i], Bandwidth=requests[i].Bandwidth, AllocationId=requests[i].RequestId ?? yeni tam Guid, AllocationTime=DateTime.Now}
```

> Not: `CalculateParticleFitness` içinde `tempBlocks`'a sabit `Bandwidth = 0.02` verilmesi orijinal kodda olduğu gibi bir tutarsızlıktır (gerçek istek bant genişliği yerine sabit değer) — birebir korunmalıdır.

---

## 9. Web Arayüzü: ASP.NET Core Web API + React/Next.js (Windows Forms YERİNE)

Eski Windows Forms masaüstü arayüzü tamamen kaldırılmıştır. Yerine iki parça gelir: (A) çekirdek kütüphaneyi HTTP üzerinden sunan bir **ASP.NET Core Minimal API** ve (B) bunu tüketen, **modern görünümlü, responsive** bir **Next.js (React, en güncel sürüm)** web arayüzü.

### 9.1 Oturum Modeli

Motor (`IntelligentFrequencyAllocator`) durumsaldır (state'li) — kurucuda `minFreq`/`maxFreq` alır, sonrasında tahsisleri kendi içinde biriktirir. Bu, tek kullanıcılı/demo amaçlı bir API için **tek, süreç içi (in-memory) singleton oturum** olarak modellenir:

```csharp
public class AllocationSessionService
{
    private readonly object _lock = new();
    private IntelligentFrequencyAllocator? _allocator;

    public bool IsInitialized => _allocator is not null;

    public RangeResponse Initialize(double minFreqGHz, double maxFreqGHz)
    {
        lock (_lock)
        {
            _allocator = new IntelligentFrequencyAllocator(minFreqGHz, maxFreqGHz);
            return new RangeResponse(minFreqGHz, maxFreqGHz);
        }
    }

    public RangeResponse? GetRange()
    {
        lock (_lock)
            return _allocator is null ? null
                : new RangeResponse(_allocator.MinFrequency, _allocator.MaxFrequency);
    }

    public AllocatedBlockDto? Allocate(double bandwidthMHz)
    {
        lock (_lock)
        {
            EnsureInitialized();
            var request = new FrequencyAllocationRequest
            {
                Bandwidth = bandwidthMHz / 1000.0,
                RequestId = Guid.NewGuid().ToString("N")[..8],
                RequestTime = DateTime.Now,
                Priority = 1
            };
            var block = _allocator!.AllocateFrequency(request);
            return block is null ? null : ToDto(block);
        }
    }

    public BatchResultDto AllocateBatch(IReadOnlyList<double> bandwidthsMHz)
    {
        lock (_lock)
        {
            EnsureInitialized();
            var requests = bandwidthsMHz.Select(mhz => new FrequencyAllocationRequest
            {
                Bandwidth = mhz / 1000.0,
                RequestId = Guid.NewGuid().ToString("N")[..8],
                RequestTime = DateTime.Now,
                Priority = 1
            }).ToList();

            var result = _allocator!.AllocateBatch(requests);
            return new BatchResultDto(
                result.SuccessfulAllocations.Select(ToDto).ToList(),
                result.FailedRequests.Select(r => r.Bandwidth * 1000.0).ToList(),
                result.TotalFragmentation,
                result.OptimizationScore);
        }
    }

    public List<AllocatedBlockDto> GetAllocations()
    {
        lock (_lock) { EnsureInitialized(); return _allocator!.GetAllocatedBlocks().Select(ToDto).ToList(); }
    }

    public bool Deallocate(string id)
    {
        lock (_lock)
        {
            EnsureInitialized();
            var existed = _allocator!.GetAllocatedBlocks().Any(b => b.AllocationId == id);
            _allocator.DeallocateFrequency(id);
            return existed;
        }
    }

    public double GetUtilization()
    {
        lock (_lock) { EnsureInitialized(); return _allocator!.GetSpectrumUtilization(); }
    }

    private void EnsureInitialized()
    {
        if (_allocator is null) throw new InvalidOperationException("Session not initialized");
    }

    private static AllocatedBlockDto ToDto(AllocatedBlock b) => new(
        b.AllocationId, b.CenterFrequency, b.Bandwidth * 1000.0, b.StartFreq, b.EndFreq, b.AllocationTime);
}
```

> Not: `RequestId` üretimi artık **backend'de, hem tekil hem toplu tahsis için tutarlı biçimde** (8 karakterlik kısa GUID) yapılır — eski WinForms'daki "tekil = tam GUID, toplu = kısa GUID" tutarsızlığı ortadan kalkmıştır (bkz. bölüm 10-B.2).

### 9.2 DTO'lar (`Dtos.cs`)

```csharp
public record RangeRequest(double MinFreqGHz, double MaxFreqGHz);
public record RangeResponse(double MinFreqGHz, double MaxFreqGHz);
public record AllocateRequest(double BandwidthMHz);
public record BatchAllocateRequest(List<double> BandwidthsMHz);
public record AllocatedBlockDto(
    string AllocationId,
    double CenterFrequencyGHz,
    double BandwidthMHz,
    double StartFreqGHz,
    double EndFreqGHz,
    DateTime AllocationTime);
public record BatchResultDto(
    List<AllocatedBlockDto> SuccessfulAllocations,
    List<double> FailedBandwidthsMHz,
    double TotalFragmentationGHz,
    double OptimizationScore);
public record UtilizationResponse(double UtilizationPercent);
public record ErrorResponse(string Message);
```

### 9.3 `Program.cs` (Minimal API)

```csharp
using AIDrivenFrequencyAllocation;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddCors(options => options.AddDefaultPolicy(policy =>
    policy.WithOrigins(builder.Configuration.GetSection("AllowedOrigins").Get<string[]>()
              ?? new[] { "http://localhost:3000" })
          .AllowAnyHeader()
          .AllowAnyMethod()));

builder.Services.AddSingleton<AllocationSessionService>();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

app.UseCors();
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.MapPost("/api/session", (RangeRequest req, AllocationSessionService svc) =>
{
    if (req.MinFreqGHz >= req.MaxFreqGHz) return Results.BadRequest(new ErrorResponse("minFreqGHz maxFreqGHz'den küçük olmalı"));
    return Results.Ok(svc.Initialize(req.MinFreqGHz, req.MaxFreqGHz));
});

app.MapGet("/api/session", (AllocationSessionService svc) =>
    svc.GetRange() is { } range ? Results.Ok(range) : Results.NotFound(new ErrorResponse("Oturum başlatılmadı")));

app.MapPost("/api/allocations", (AllocateRequest req, AllocationSessionService svc) =>
{
    if (!svc.IsInitialized) return Results.BadRequest(new ErrorResponse("Önce /api/session ile aralık ayarlayın"));
    var block = svc.Allocate(req.BandwidthMHz);
    return block is null
        ? Results.UnprocessableEntity(new ErrorResponse("Uygun frekans bulunamadı"))
        : Results.Ok(block);
});

app.MapPost("/api/allocations/batch", (BatchAllocateRequest req, AllocationSessionService svc) =>
{
    if (!svc.IsInitialized) return Results.BadRequest(new ErrorResponse("Önce /api/session ile aralık ayarlayın"));
    return Results.Ok(svc.AllocateBatch(req.BandwidthsMHz));
});

app.MapGet("/api/allocations", (AllocationSessionService svc) =>
    svc.IsInitialized ? Results.Ok(svc.GetAllocations()) : Results.BadRequest(new ErrorResponse("Önce /api/session ile aralık ayarlayın")));

app.MapDelete("/api/allocations/{id}", (string id, AllocationSessionService svc) =>
    svc.Deallocate(id) ? Results.NoContent() : Results.NotFound());

app.MapGet("/api/utilization", (AllocationSessionService svc) =>
    svc.IsInitialized ? Results.Ok(new UtilizationResponse(svc.GetUtilization())) : Results.BadRequest(new ErrorResponse("Önce /api/session ile aralık ayarlayın")));

app.Run();
```

### 9.4 API Uç Nokta Özeti

| Metod | Yol | Gövde | Yanıt | Notlar |
|---|---|---|---|---|
| POST | `/api/session` | `{ minFreqGHz, maxFreqGHz }` | `RangeResponse` | Yeni/sıfırlanmış oturum başlatır (eski WinForms'daki başlangıç diyaloğunun yerini alır) |
| GET | `/api/session` | — | `RangeResponse` veya 404 | Aralık bilgisini döner (reflection YOK, `MinFrequency`/`MaxFrequency` property üzerinden) |
| POST | `/api/allocations` | `{ bandwidthMHz }` | `AllocatedBlockDto` veya 422 | Tekil tahsis |
| POST | `/api/allocations/batch` | `{ bandwidthsMHz: number[] }` | `BatchResultDto` | Toplu tahsis |
| GET | `/api/allocations` | — | `AllocatedBlockDto[]` | Tüm tahsisler |
| DELETE | `/api/allocations/{id}` | — | 204 veya 404 | Tahsisi kaldırır |
| GET | `/api/utilization` | — | `UtilizationResponse` | Anlık spektrum kullanım yüzdesi |

### 9.5 Next.js Uygulaması — Genel Yaklaşım

- **Next.js en güncel sürüm, App Router** (`app/` dizini), **TypeScript**, **Tailwind CSS**.
- Ana ekran istemci bileşeni (`"use client"`) olarak çalışır; sayfa `app/page.tsx` içinde ya `RangeSetupScreen` ya da (oturum zaten kurulmuşsa) dashboard gösterilir.
- API taban adresi `NEXT_PUBLIC_API_BASE_URL` ortam değişkeninden okunur (`web/.env.local`).
- Veri senkronizasyonu: her mutasyon (tahsis/toplu tahsis/kaldırma) sonrası ilgili veriler yeniden çekilir (fetch-then-refetch); ayrıca çoklu sekme/istemci senkronizasyonu için arka planda **2 saniyede bir** hafif bir polling (`/api/allocations`, `/api/utilization`) yapılır. Eski WinForms'daki 100ms'lik UI-timer'ın (yerel bellek üzerinde çizim yeniden tetikleme) aksine, artık ağ üzerinden veri çekildiği için makul bir aralık seçilmiştir — bu bilinçli bir modernizasyon kararıdır.

### 9.6 Bileşenler ve Modern Görsel Tasarım

Genel tasarım dili: **temiz, minimal, kart tabanlı (card-based) bir dashboard**; `shadcn/ui` bileşenleri (Card, Button, Input, Dialog, Progress, Badge, Toast/Sonner) ve Tailwind ile tutarlı boşluk/tipografi/renk sistemi. `next-themes` ile açık/koyu tema desteği (sistem tercihine duyarlı, sağ üstte tema anahtarı). İkonlar için `lucide-react` (ör. `Radio`, `Trash2`, `Layers`, `Gauge`, `CheckCircle2`, `XCircle`).

**`RangeSetupScreen`** — eski başlangıç diyaloğunun (min/max GHz + "Başlat") yerini alır. Ortalanmış, gradient/blur arka planlı bir kart: "Min Frekans (GHz)" ve "Max Frekans (GHz)" sayısal inputları (varsayılan 2 / 18), "Başlat" butonu → `POST /api/session`. Geçersiz aralık (min ≥ max veya parse hatası) durumunda inline hata mesajı / toast (eski "Geçersiz frekans aralığı!" mesajının modern karşılığı).

**Dashboard düzeni** (responsive grid, eski WinForms yerleşiminin oranlarını andıran ama akışkan bir modern karşılığı):

```
┌───────────────────────────────────────────────────────────┐
│  Üst bar: başlık "Akıllı Frekans Tahsis Sistemi" + tema    │
│  anahtarı + aralık rozeti (ör. "2.0 – 18.0 GHz")           │
├───────────────────────────────────────────────────────────┤
│  SpectrumVisualizer (geniş kart, tüm genişlik)              │
├───────────────────────────────┬─────────────────────────────┤
│  AllocationList (kart, sol,   │  AllocationControlPanel      │
│  ~%65 genişlik)                │  (kart, sağ, ~%35 genişlik) │
│                                │   - Bant genişliği inputu   │
│                                │   - "Frekans Tahsis Et"     │
│                                │   - "Toplu Tahsis" (dialog) │
│                                │   - Seçili satırı kaldır    │
│                                │   - UtilizationGauge        │
├───────────────────────────────┴─────────────────────────────┤
│  Alt durum çubuğu: aralık + toplam tahsis sayısı + son       │
│  optimizasyon skoru                                          │
└───────────────────────────────────────────────────────────┘
```

Küçük ekranlarda (mobil) sütunlar alt alta yığılır (`grid-cols-1 lg:grid-cols-[65%_35%]` benzeri bir Tailwind grid).

**`SpectrumVisualizer`** — eski `SpectrumPictureBox_Paint`'in (GDI+) doğrudan web karşılığı, **inline SVG** ile (harici grafik kütüphanesi gerekmez):

- `viewBox` tabanlı, konteyner genişliğine göre ölçeklenen responsive bir `<svg>`.
- Arka planda 11 dikey ızgara çizgisi + altında frekans etiketleri (`minFreq + (range/10)*i`, 1 ondalık), eski Paint mantığıyla birebir aynı hesaplama.
- Her `AllocatedBlock` için: yarı saydam dolgulu bir `<rect>` (StartFreq→EndFreq aralığı), kalın kenarlıklı çerçeve, kesikli (`strokeDasharray`) dikey merkez çizgisi, ve blok üzerinde/içinde `{CenterFrequency:F2} GHz` + `{BandwidthMHz:F0} MHz` etiketi (arkasında okunabilirlik için yarı saydam kart/arka plan).
- Renk paleti: index'e göre döngüsel 10 renklik **kategorik** bir palet (eski `Blue,Red,Green,Orange,Purple,Brown,Pink,Cyan,Magenta,Yellow` paletinin yerini, açık/koyu temada da okunaklı, erişilebilir kontrastlı modern bir kategorik palet alır — ör. Tailwind'in `blue-500, rose-500, emerald-500, amber-500, violet-500, orange-500, pink-500, cyan-500, fuchsia-500, yellow-500` tonları).
- Boş durum (hiç tahsis yokken): "Henüz tahsis yok" şeklinde nazik bir boş-durum (empty state) mesajı/illüstrasyonu.
- Hover ile bir bloğun üzerine gelindiğinde tam detay (ID, aralık, süre) gösteren bir tooltip (isteğe bağlı geliştirme, temel gereksinim değil ama "modern görünüm" beklentisiyle uyumludur).

**`AllocationList`** — tahsis listesini modern bir tablo/kart-listesi olarak gösterir (eski `ListBox`'ın yerini alır): her satırda renkli nokta/rozet (blok rengiyle eşleşen), ID, merkez frekans, bant genişliği, aralık; satır seçilebilir (radio/checkbox veya tıklanabilir satır vurgusu) ve seçili satır "Seçili Tahsisi Kaldır" butonuyla `DELETE /api/allocations/{id}` çağrısını tetikler.

**`AllocationControlPanel`** — bant genişliği (MHz) sayısal input (varsayılan 200), "Frekans Tahsis Et" birincil buton (`POST /api/allocations`), "Toplu Tahsis Et" butonu (dialog açar), "Seçili Tahsisi Kaldır" (tehlike/destructive renk varyantı, sadece bir satır seçiliyken aktif). Bilgilendirici bir not: **hem virgülle hem satır satır ayrılmış girişleri kabul eden toplu tahsis** özelliğine referans (bkz. `BatchAllocationDialog`) — eski metin/davranış tutarsızlığı çözülmüştür (bkz. bölüm 10-B.3).

**`BatchAllocationDialog`** — modal (`shadcn/ui Dialog`): çok satırlı `textarea`, kullanıcı bant genişliklerini **hem yeni satırla hem virgülle** ayırabilir (esnek ayrıştırma: girdi önce satırlara, sonra her satır virgüle göre bölünür, boşluklar `trim` edilir, sayıya çevrilemeyenler yok sayılır). Ayrıca eskiden gizli olan **"Rastgele Doldur"** özelliği artık **görünür ve kullanılabilir** bir alt bölüm olarak sunulur: Adet / Min MHz / Max MHz sayısal inputları + "Rastgele Doldur" butonu, textarea'yı otomatik doldurur (bkz. bölüm 10-B.4). "Toplu Tahsis Yap" butonu `POST /api/allocations/batch` çağırır; sonuç modal kapanmadan, dialog içinde bir özet kart olarak gösterilir: başarılı/başarısız sayıları (ikon + sayı rozetleri, ör. `CheckCircle2`/`XCircle`), fragmentasyon (MHz cinsinden "Ortalama Boşluk" olarak, ör. `Ortalama Boşluk: 12.3 MHz`), optimizasyon skoru (0-100, renkli `Progress` çubuğu). "Kapat" butonuyla dialog kapanır ve ana liste/spektrum yeniden çekilir.

**`UtilizationGauge`** — `GetSpectrumUtilization()` sonucunu modern bir dairesel/yatay `Progress` göstergesiyle sunar: %80 üzeri kırmızı, %50-80 turuncu/amber, altı yeşil (eski WinForms renk eşiklerinin birebir aynısı, sadece modern bir gauge bileşeniyle).

**Bildirimler** — `sonner` (toast) ile: tahsis başarılı ("Frekans tahsis edildi: X GHz"), tahsis başarısız ("Uygun frekans bulunamadı"), toplu tahsis özeti, kaldırma onayı, ağ/oturum hataları. Eski WinForms'daki yorum satırına alınmış (devre dışı) başarı `MessageBox`'larının aksine, yeni arayüzde **hem başarı hem hata bildirimleri aktif olarak gösterilir** (bkz. bölüm 10-B.5) — bu, "modern ve kullanıcı dostu" beklentisiyle bilinçli bir iyileştirmedir.

### 9.7 `lib/types.ts` (özet)

```typescript
export interface RangeResponse { minFreqGHz: number; maxFreqGHz: number }
export interface AllocatedBlockDto {
  allocationId: string
  centerFrequencyGHz: number
  bandwidthMHz: number
  startFreqGHz: number
  endFreqGHz: number
  allocationTime: string
}
export interface BatchResultDto {
  successfulAllocations: AllocatedBlockDto[]
  failedBandwidthsMHz: number[]
  totalFragmentationGHz: number
  optimizationScore: number
}
export interface UtilizationResponse { utilizationPercent: number }
```

### 9.8 Ortam Değişkenleri

```
# web/.env.local
NEXT_PUBLIC_API_BASE_URL=http://localhost:5179
```

```json
// AIDrivenFrequencyAllocation.Api/appsettings.Development.json (özet)
{
  "AllowedOrigins": ["http://localhost:3000"]
}
```

---

## 10. Bilinen Kusurlar / Tutarsızlıklar

### 10-A. Çekirdek Algoritmada — KORUNMASI Gereken (Bölüm 3-8, backend mantığı, teknoloji değişikliğinden bağımsız)

Bunlar hata değil, **orijinal algoritmanın mevcut halidir**; arayüz teknolojisi değişse de bu davranışlar birebir korunmalıdır (aksi açıkça istenmedikçe):

1. Simulated annealing'de olasılıksal kötü-çözüm kabulü yorum satırında, aktif değil (bkz. 5.2).
2. `CalculateCost` içindeki "kenar yakınlığı cezası" ve "verimlilik bonusu" tamamen yorum satırında (bkz. 5.5).
3. Tekil (`FindAvailableGaps`, guardBand×1) ve toplu (`FindAvailableGapsWithBlocks`, guardBand×2) boşluk bulma fonksiyonlarında kenar boşluğu eşiği farklıdır.
4. `CalculateTotalFragmentation()` boş liste ile `CalculateBatchFragmentation`'ı çağırır (bkz. 6.8 açıklaması) — isim yanıltıcı ama işlevsel.
5. `FindOptimalBatchAllocation` (PSO), `CalculateBatchOptimizationScore` ve `CalculateSpectrumEfficiency` tanımlı ama hiçbir aktif akıştan çağrılmıyor (dead code, bkz. bölüm 8).
6. `CalculateParticleFitness` içinde PSO parçacıklarının geçici bloklarına gerçek bant genişliği yerine sabit `0.02` GHz veriliyor.

### 10-B. Eski Windows Forms Arayüzüne Özgüydü — Yeni React/Next.js Mimarisinde ÇÖZÜLDÜ

Bu maddeler eski masaüstü UI'nin (artık var olmayan `FreqAllocationUI` projesinin) kusurlarıydı. Yeni mimaride her biri açıkça ele alınmıştır:

1. `TotalFragmentation` bir GHz değeri iken eski UI'da `:P1` (yüzde) formatıyla yanıltıcı şekilde gösteriliyordu → yeni arayüzde MHz cinsinden "Ortalama Boşluk" olarak, optimizasyon skoru ise ayrı bir gösterge olarak sunulur (bkz. 9.6, `BatchAllocationDialog` ve durum çubuğu).
2. Tekil tahsiste tam GUID, toplu tahsis dialogunda 8 karakterlik kısaltılmış GUID `RequestId` kullanılıyordu → yeni backend'de (`AllocationSessionService`) her iki akış için de tutarlı biçimde 8 karakterlik kısa kimlik üretilir (bkz. 9.1).
3. Eski `infoLabel` metni "virgülle ayırın" diyordu ama gerçek parse mantığı satır bazlıydı → yeni `BatchAllocationDialog`, hem virgülü hem satır sonunu kabul eden esnek bir ayrıştırıcı kullanır ve arayüz metni bunu doğru yansıtır (bkz. 9.6).
4. Toplu tahsis dialogundaki hızlı-doldurma kontrolleri (Adet/Min/Max MHz + "Rastgele Doldur") kodda tam işlevseldi ama `Visible=false` ile kullanıcıdan gizlenmişti → yeni arayüzde bu özellik görünür ve kullanılabilir bir alt bölüm olarak sunulur (bkz. 9.6).
5. Eski koddaki başarı/bilgi `MessageBox` çağrıları yorum satırına alınmıştı (tahsis/kaldırma sessizce gerçekleşiyordu) → yeni arayüzde hem başarı hem hata durumları toast bildirimleriyle aktif olarak gösterilir (bkz. 9.6).
6. `FreqAllocationUI/Program.cs` dosyasındaki Türkçe metinler mojibake (bozuk kodlama) olarak kaydedilmişti → bu dosya artık mevcut değil; yeni `RangeSetupScreen` (React/TypeScript, UTF-8) doğru Türkçe metinlerle yazılır ("Frekans Aralığı Ayarları", "Başlat", "Geçersiz frekans aralığı!").
7. Eski UI, allocator'ın private `minFreq`/`maxFreq` alanlarını .NET reflection ile okuyordu (kırılgan, alan adı değişirse bozulur) → çekirdek kütüphaneye public `MinFrequency`/`MaxFrequency` salt-okunur property'leri eklendi (bölüm 4) ve API bunları DTO üzerinden düzgünce dışa açar (bölüm 9.1-9.4); reflection tamamen kaldırılmıştır.

---

## 11. Derleme ve Çalıştırma

```bash
# --- Backend ---
# Çözümü derle (çekirdek kütüphane + Web API)
dotnet build AIDrivenFrequencyAllocation.sln

# Web API'yi çalıştır (varsayılan geliştirme portu, örn. http://localhost:5179)
dotnet run --project AIDrivenFrequencyAllocation.Api

# --- Frontend ---
cd web
npm install
npm run dev
# http://localhost:3000 üzerinden erişilebilir; NEXT_PUBLIC_API_BASE_URL, web/.env.local içinde
# Web API adresini göstermelidir (bkz. bölüm 9.8).
```

Next.js uygulaması açıldığında önce `RangeSetupScreen` (min/max GHz kurulumu) gösterilir; "Başlat" ile `POST /api/session` çağrılır ve ardından dashboard'a geçilir (bkz. bölüm 9.6).

## 12. Gereksinimler

- **.NET 8 SDK** — hem çekirdek kütüphane hem Web API için; **Windows'a özgü hiçbir bağımlılık yoktur**, backend Linux/macOS/Windows'ta çalışabilir.
- **Node.js** (Next.js'in güncel sürümünün gerektirdiği en güncel LTS sürüm) ve **npm** (veya pnpm/yarn) — Next.js uygulaması için.
- Modern bir web tarayıcısı (arayüz artık bir masaüstü uygulaması değil, tarayıcıda çalışan bir web uygulamasıdır).
