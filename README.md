# 🏰 Belvedere

**The Fortress for Your Memories.**

**Belvedere** is an open-source, self-hosted, zero-trust photo gallery designed for those who refuse to compromise on privacy. Built with a "Security-First" architecture, it allows you to host, manage, and share high-resolution imagery with customers or friends without ever surrendering ownership of your data or metadata.

---

## 🛡️ The Zero-Trust Philosophy
Most photo galleries are built for convenience; Belvedere is built for **integrity**. 

*   **Server-Blind Privacy:** Designed to support client-side encryption—what the server can’t read, the server can’t leak.
*   **BFF Architecture:** Using a Backend-for-Frontend pattern to eliminate sensitive token storage in the browser.
*   **Ephemeral Sharing:** Share albums with expiring, cryptographically signed URLs. Once the time is up, the access is gone forever.
*   **No Crawlers, No Leaks:** Built-in defenses against scrapers and unauthorized indexing.

---

## ✨ Key Features

*   📸 **High-Performance Rendering:** WebGL-accelerated image viewing for a smooth experience even with massive raw files.
*   🔒 **True Multi-Tenancy:** Robust isolation between users using Database Row-Level Security (RLS).
*   🎨 **Pro-Grade Management:** Organize by metadata, EXIF data, and custom albums with ease.
*   🤝 **Secure Client Delivery:** Perfect for photographers needing to share galleries with clients via secure, passwordless (Passkey/WebAuthn) or time-limited access.
*   📱 **Responsive & Fast:** A modern React frontend following the Bulletproof React pattern.

---

## 🏗️ Tech Stack

Belvedere utilizes a decoupled, enterprise-grade stack:

| Component | Technology | Description |
| :--- | :--- | :--- |
| **Frontend** | **React (Vite)** | Following **Bulletproof React** architecture for maximum maintainability. |
| **Backend** | **ASP.NET Core** | A high-performance C# API acting as a secure **BFF**. |
| **Database** | **PostgreSQL** | Utilized with **Row-Level Security** (RLS) for absolute data isolation. |
| **Object Storage** | **MinIO / S3** | S3-compatible storage with time-limited signed URL generation. |
| **Auth** | **OpenID Connect** | Integration with self-hosted identity providers via HttpOnly cookies. |

---

## 🔒 Security Hardening

Belvedere is designed to be "Secure by Default."

> **Warning:** To maintain the Zero-Trust guarantee, ensure your production environment is served over HTTPS with a strict Content Security Policy (CSP).

*   **Cookie-based Auth:** We use `SameSite=Strict`, `HttpOnly`, `Secure` cookies to mitigate XSS risks.
*   **Anti-Forgery:** Built-in CSRF protection for all state-changing operations via header-token validation.
*   **Signed Media:** Direct file paths are never exposed; all media is streamed via authenticated, expiring proxies.

---

## 🤝 Contributing

We welcome contributions from the privacy-conscious community! Please see our `CONTRIBUTING.md` for guidelines on how to submit security audits, bug fixes, or feature requests.

---

## 📜 License

Belvedere is open-source software licensed under the **MIT License**.

---

**Built with 🛡️ by Elija Lambourne**
*Because your memories shouldn't be someone else's training data.*
