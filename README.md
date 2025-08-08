# 🛍️ WebShop

A full-stack webshop application built with:

- ⚙️ **Backend:** ASP.NET Core Web API + PostgreSQL  
- 🌐 **Frontend:** Next.js (React) + Tailwind CSS + TypeScript  
- 🐘 **Database:** PostgreSQL (local dev with Docker or native install)

---

## 🗂️ Project Structure

WebShop/
├── client/ # Next.js frontend
├── api/ # ASP.NET Core Web API backend
│ └── Webshop.Api/
├── db/ # (Optional) SQL scripts or seed data
└── README.md

## 🚀 Getting Started

### ✅ Requirements

- [.NET 8 SDK](https://dotnet.microsoft.com/en-us/download)
- [Node.js](https://nodejs.org/)
- [PostgreSQL](https://www.postgresql.org/download/) or Docker
- (Optional) Docker & Docker Compose

---

## ⚙️ Backend Setup

### 📦 Install Dependencies

```bash
cd api/Webshop.Api
dotnet restore

"ConnectionStrings": {
  "DefaultConnection": "Host=localhost;Port=5432;Database=webshop;Username=postgres;Password=yourpassword"
}

🛠️ Run Migrations

dotnet ef migrations add InitialCreate
dotnet ef database update

▶️ Run the API

bash
Kopiér
Rediger
dotnet run
Visit Swagger UI: https://localhost:5001/swagger

🌐 Frontend Setup

cd client
npm install
npm run dev
Frontend runs at: http://localhost:3000

📡 API Endpoints

Method	Route	Description
GET	/api/products	Get all products
POST	/api/products	Add new product

📦 Features
🛍️ Browse products

🧺 Add to cart with quantity

📊 View cart total

📡 RESTful API

🎨 Styled with Tailwind

🔐 Ready for authentication & checkout integration

🚀 Deployment (Coming Soon)

Azure App Service (API)

Azure Static Web Apps (Frontend)

Azure Database for PostgreSQL Flexible Server

Docker support

🧠 Ideas for Next

🔐 Auth (OAuth/JWT)

💳 Stripe/PayPal checkout

🧾 Order history

📦 Admin dashboard

📥 Product reviews

📄 License

MIT — Free for personal or commercial use.

