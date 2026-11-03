export interface Product {
  id: number;
  name: string;
  price: number;
  stock: number;
  createdAt: string;
  updatedAt: string;
}

export interface CreateProductRequest {
  name: string;
  price: number;
  stock: number;
}

export interface UpdateProductRequest {
  name: string;
  price: number;
}

export interface PagedResult<T> {
  items: T[];
  page: number;
  pageSize: number;
  totalCount: number;
  totalPages: number;
  hasPrevious: boolean;
  hasNext: boolean;
}