# Blockchain-Based Diploma Verification System (MVP)

![.NET](https://img.shields.io/badge/.NET-10.0-512BD4?style=for-the-badge&logo=dotnet)
![Solidity](https://img.shields.io/badge/Solidity-Smart%20Contract-363636?style=for-the-badge&logo=solidity)
![Ethereum](https://img.shields.io/badge/Ethereum-Sepolia-627EEA?style=for-the-badge&logo=ethereum)
![Status](https://img.shields.io/badge/Status-MVP-success?style=for-the-badge)

## 1. Proje Özeti

**Blockchain-Based Diploma Verification System (MVP)**, PDF formatındaki diplomaların bütünlüğünü ve doğrulanabilirliğini Ethereum Sepolia ağı üzerinde sağlayan bir doğrulama sistemidir.

Bu sistemde diplomanın orijinal PDF içeriği blockchain'e kaydedilmez. Bunun yerine, PDF dosyasından üretilen **SHA-256 hash değeri** akıllı sözleşme üzerinde saklanır. Böylece:

- Diploma içeriği gizli kalır.
- Blockchain üzerinde değiştirilemez, timestamp içeren bir kayıt oluşur.
- PDF üzerinde yapılacak en küçük değişiklik farklı bir hash üreteceği için belge doğrulaması başarısız olur.

Bu yaklaşım, **privacy-preserving verification** ve **immutable proof of existence** prensiplerini bir araya getirir.

## 2. Temel Özellikler

| Özellik | Açıklama |
| --- | --- |
| **Immutable Storage** | Diploma hash kayıtları Ethereum Sepolia Testnet üzerinde smart contract aracılığıyla tutulur. |
| **SHA-256 Hashing** | PDF dosyasının içeriğinden deterministik SHA-256 hash değeri üretilir. |
| **Otomatik QR Kod** | Başarılı kayıt sonrası doğrulama URL'si için otomatik QR kod oluşturulur. |
| **Mükerrer Kayıt Engelleme** | Aynı PDF hash değerinin ikinci kez kaydedilmesi smart contract seviyesinde engellenir. |
| **Gerçek Zamanlı Doğrulama** | Yüklenen PDF'in hash değeri blockchain kaydıyla anlık olarak karşılaştırılır. |
| **Gizlilik Odaklı Tasarım** | Blockchain üzerinde yalnızca hash ve timestamp saklanır; diploma dosyasının içeriği kaydedilmez. |

## 3. Teknoloji Yığını

| Katman | Teknolojiler |
| --- | --- |
| **Backend** | ASP.NET Core 10, C#, Nethereum, QRCoder |
| **Blockchain** | Solidity Smart Contract, Ethereum Sepolia Testnet, Alchemy RPC |
| **Frontend** | HTML5, CSS3, JavaScript |
| **Hashing** | SHA-256 |
| **Deployment/Test** | Hardhat, Sepolia Faucet, Alchemy |

## 4. Mimari Yapı

Proje, sürdürülebilirlik ve test edilebilirlik hedeflenerek servis odaklı bir yapıda geliştirilmiştir.

| Mimari Prensip | Uygulama Yaklaşımı |
| --- | --- |
| **SOLID** | Hash üretimi, QR kod üretimi, doğrulama bağlantısı ve blockchain entegrasyonu ayrı servis sorumluluklarına bölünmüştür. |
| **Clean Architecture** | Controller katmanı iş akışını yönetir; domain davranışları servisler üzerinden izole edilir. |
| **Repository Pattern Yaklaşımı** | Blockchain smart contract, kalıcı veri kaynağı gibi ele alınır; erişim `IDiplomaBlockchainService` arayüzü arkasında soyutlanır. |
| **Service-Oriented Design** | PDF validasyonu, SHA-256 dönüşümü, Nethereum çağrıları ve QR üretimi bağımsız servisler üzerinden yürütülür. |

Temel veri akışı:

```text
Frontend
  ↓
ASP.NET Core API
  ↓
PDF Validation
  ↓
SHA-256 Hash Generation
  ↓
Nethereum Integration
  ↓
Ethereum Sepolia Smart Contract
  ↓
Verification Result + QR Code
```

## 5. Kurulum (Setup)

### Gereksinimler

- .NET 10 SDK
- Node.js ve npm
- Sepolia RPC URL bilgisi (Alchemy önerilir)
- Sepolia Test ETH
- Deploy edilmiş `DiplomaRegistry` smart contract adresi

### Backend Kurulumu

```bash
dotnet restore
```

`appsettings.Local.json` dosyasını oluşturun:

```json
{
  "Blockchain": {
    "NetworkName": "Sepolia",
    "ChainId": 11155111,
    "RpcUrl": "https://eth-sepolia.g.alchemy.com/v2/YOUR_API_KEY",
    "PrivateKey": "YOUR_PRIVATE_KEY",
    "ContractAddress": "0xYOUR_CONTRACT_ADDRESS",
    "VerificationBaseUrl": ""
  }
}
```

> `PrivateKey`, RPC URL ve API anahtarları GitHub'a yüklenmemelidir. Bu bilgiler `.gitignore` kapsamındaki local config dosyalarında tutulmalıdır.

Uygulamayı çalıştırın:

```bash
dotnet run
```

### Smart Contract Kurulumu

```bash
cd blockchain
npm install
```

`.env` dosyasını oluşturun:

```env
SEPOLIA_RPC_URL=https://eth-sepolia.g.alchemy.com/v2/YOUR_API_KEY
PRIVATE_KEY=YOUR_DEPLOYER_PRIVATE_KEY_WITHOUT_0X
```

Contract testleri:

```bash
npm test
```

Sepolia deployment:

```bash
npm run deploy:sepolia
```

## 6. Demo

### Ekran Görüntüleri

| Ekran | Beklenen Görsel |
| --- | --- |
| **Diploma Kayıt Ekranı** | `screenshots/upload-screen.png` placeholder |
| **Geçerli Diploma Sonucu** | `screenshots/valid-diploma.png` placeholder |
| **Geçersiz Diploma Sonucu** | `screenshots/invalid-diploma.png` placeholder |
| **Blockchain Kaydı Bulunamadı Sonucu** | `screenshots/blockchain-record-not-found.png` placeholder |

> Demo sunumunda aşağıdaki üç durumun ayrı ekran görüntüleriyle gösterilmesi önerilir: `Geçerli Diploma`, `Geçersiz Diploma`, `Blockchain Kaydı Bulunamadı`.

### Blockchain Transaction Kontrolü

| Alan | Placeholder |
| --- | --- |
| **Network** | Sepolia |
| **Transaction Hash** | `0x...` |
| **Contract Address** | `0x...` |
| **Explorer** | `https://sepolia.etherscan.io/tx/0x...` |

### Örnek Doğrulama Sonuçları

| Durum | Senaryo | Ekran Görüntüsü Placeholder |
| --- | --- | --- |
| `Geçerli Diploma` | Blockchain üzerinde kayıtlı, değiştirilmemiş PDF yüklendi. | `screenshots/valid-diploma.png` |
| `Geçersiz Diploma` | PDF içeriği değiştirilmiş veya dosya doğrulaması başarısız. | `screenshots/invalid-diploma.png` |
| `Blockchain Kaydı Bulunamadı` | QR/hash doğrulamasında ilgili hash için blockchain kaydı yok. | `screenshots/blockchain-record-not-found.png` |

---

Bu MVP, diploma doğrulama senaryosunda belge gizliliğini korurken blockchain'in değiştirilemez kayıt yapısından yararlanır. Production ortamı için role-based access control, contract ownership, revocation mekanizması, audit logging ve CI/CD tabanlı deployment süreçleri eklenebilir.
