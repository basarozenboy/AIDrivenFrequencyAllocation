# Frekans Tahsis Sistemi — Web Arayüzü

Bu, `AIDrivenFrequencyAllocation.Api` projesinin sunduğu REST API'yi tüketen Next.js (App Router, React 19, TypeScript, Tailwind CSS) tabanlı modern web arayüzüdür. Eski Windows Forms arayüzünün yerini alır — bkz. kök dizindeki [README.md](../README.md), bölüm 9.

## Geliştirme

```bash
npm install
npm run dev
```

`web/.env.local` içindeki `NEXT_PUBLIC_API_BASE_URL`, `AIDrivenFrequencyAllocation.Api` projesinin çalıştığı adresi göstermelidir (varsayılan: `http://localhost:5179`). API'nin de ayrıca çalışıyor olması gerekir:

```bash
# repo kökünden
dotnet run --project AIDrivenFrequencyAllocation.Api
```

Uygulama açıldığında önce frekans aralığı kurulum ekranı gösterilir; ardından spektrum görselleştirmesi, tahsis listesi ve kontrol paneliyle birlikte ana pano gelir.
