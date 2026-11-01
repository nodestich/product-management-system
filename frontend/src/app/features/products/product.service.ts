import { inject, Injectable } from "@angular/core";
import { API_BASE_URL } from "../../core/api-config";
import { HttpClient } from "@angular/common/http";
import { CreateProductRequest, Product, UpdateProductRequest } from "./product.models";
import { Observable } from "rxjs";

@Injectable({ providedIn: 'root' })
export class ProductService {
    private readonly base = `${inject(API_BASE_URL)}/api/products`;
    private readonly http = inject(HttpClient);

    getAll(): Observable<Product[]> {
        return this.http.get<Product[]>(this.base);
    }

    getById(id: number): Observable<Product> {
        return this.http.get<Product>(`${this.base}/${id}`);
    }

    create(body: CreateProductRequest): Observable<Product> {
        return this.http.post<Product>(this.base, body);
    }

    update(id: number, body: UpdateProductRequest): Observable<Product> {
        return this.http.put<Product>(`${this.base}/${id}`, body);
    }

    delete(id: number): Observable<void> {
        return this.http.delete<void>(`${this.base}/${id}`);

    }
    decrementStock(id: number, qty: number): Observable<Product> {
        return this.http.post<Product>(`${this.base}/${id}/decrement-stock/${qty}`, null);
    }

    addToStock(id: number, qty: number): Observable<Product> {
        return this.http.post<Product>(`${this.base}/${id}/add-to-stock/${qty}`, null);
    }

    search(name: string): Observable<Product[]> {
        return this.http.get<Product[]>(`${this.base}/search`, { params: { name } });
    }

    byStockRange(min: number, max: number): Observable<Product[]> {
        return this.http.get<Product[]>(`${this.base}/stock-level`, { params: { min, max } });
    }
}