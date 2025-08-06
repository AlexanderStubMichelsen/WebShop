// src/lib/products.ts
export interface Product {
  id: number;
  name: string;
  description: string;
  price: number;
  imageUrl: string;
}

export const mockProducts: Product[] = [
  {
    id: 1,
    name: "Eco-Friendly Backpack",
    description: "Durable and stylish, made from recycled materials.",
    price: 49.99,
    imageUrl: "https://picsum.photos/300",
  },
  {
    id: 2,
    name: "Noise Cancelling Headphones",
    description: "High-quality sound and comfort for work or play.",
    price: 129.99,
    imageUrl: "https://picsum.photos/300",
  },
  {
    id: 3,
    name: "Smart LED Lamp",
    description: "Touch control and adjustable lighting modes.",
    price: 24.99,
    imageUrl: "https://picsum.photos/300",
  },
  {
    id: 4,
    name: "Minimalist Wristwatch",
    description: "Sleek and simple design with a leather strap.",
    price: 89.99,
    imageUrl: "https://picsum.photos/300",
  },
];
