# 🛒 WebShop

A full-stack **TypeScript** e-commerce application built with **Next.js**, **ASP.NET Core**, and **SQLite**.  
It provides a responsive, modern shopping experience with secure payments via Stripe, persistent shopping carts, and an admin-friendly backend API.

---

## 🚀 Features

### Frontend (Next.js + TypeScript + Tailwind CSS)
- Responsive, mobile-first design
- Product catalog with images, prices, and descriptions
- Shopping cart with quantity updates and item removal
- Checkout flow with Stripe integration
- Session-based login/logout with JWT authentication
- Deployed via **Apache** on Ubuntu server

### Backend (ASP.NET Core API)
- RESTful endpoints for products, users, cart, and payments
- SQLite database with EF Core migrations
- Secure authentication with JWT & bcrypt password hashing
- Admin endpoints for product management
- Health checks and metrics via App.Metrics + Prometheus
- Integration tests with xUnit

### Database (SQLite)
- Lightweight, file-based database (`app.db`)
- Products table with seed data
- User accounts with secure password storage
- Orders & order items for purchase history
- No separate database server required

---

## 🗂️ Tech Stack

| Layer       | Technology |
|-------------|------------|
| Frontend    | Next.js, React, TypeScript, Tailwind CSS |
| Backend     | ASP.NET Core 8, C# |
| Database    | SQLite |
| Payments    | Stripe API |
| Deployment  | Apache2 (Frontend), systemd (Backend) |
| CI/CD       | GitHub Actions (SCP deployment to server) |
| Testing     | xUnit, EF Core in-memory/SQLite |

---

## ⚙️ Setup & Installation

### 1️⃣ Clone the Repository
```bash
git clone https://github.com/AlexanderStubmichelsen/webshop.git
cd webshop

2️⃣ Configure Environment Variables
Backend (.env)
DATABASE_PATH=app.db
STRIPE_SECRET_KEY=sk_test_...
STRIPE_PUBLISHABLE_KEY=pk_test_...
JWT_SECRET=your_jwt_secret

Frontend (.env.local)
NEXT_PUBLIC_API_URL=http://localhost:5019
NEXT_PUBLIC_STRIPE_PUBLISHABLE_KEY=pk_test_...

▶️ Running the Project

Start Backend Only
cd api/Webshop.Api
dotnet restore
dotnet ef database update
dotnet run

Start Frontend Only
cd client
npm install
npm run dev

Start Both Frontend & Backend
npm run start:all
This command runs both the ASP.NET Core API and the Next.js frontend concurrently for development.

📦 Deployment
Frontend
Build with:
npm run build
Deploy out/ directory to /var/www/html (Apache or Nginx)

Backend
Publish with:
dotnet publish -c Release -o publish
Deploy publish/ to server backend directory

Ensure app.db file is placed in the correct server directory

Restart backend service:
systemctl restart webshop-backend

🧪 Testing
Run backend tests:
cd api/Webshop.Api.Tests
dotnet test

📸 Screenshots
(Add screenshots of homepage, product page, cart, and checkout here)

📜 License
This project is licensed under the MIT License.

---

If you want, I can also **add a “Development Workflow” section** to explain how contributors should branch, commit, and push so `npm run start:all` works without breaking anything in production. That would make the README feel more “production-grade.”
