import { Component, OnInit, computed, inject, signal } from '@angular/core';
import { ProductService } from '../product.service';
import { Product } from '../product.models';
import { MatDialog } from '@angular/material/dialog';
import { Observable, finalize, map } from 'rxjs';
import { ProductFormDialog } from '../product-form-dialog/product-form-dialog';
import { CurrencyPipe, DatePipe } from '@angular/common';
import { MatTableModule } from '@angular/material/table';
import { MatToolbarModule } from '@angular/material/toolbar';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatProgressBarModule } from '@angular/material/progress-bar';
import { MatIconModule } from '@angular/material/icon';
import { MatButtonModule } from '@angular/material/button';
import { MatPaginatorModule, PageEvent } from '@angular/material/paginator';
import { ConfirmDialog } from '../../shared/confirm-dialog/confirm-dialog';
import { MatSnackBar } from '@angular/material/snack-bar';
import { AuthService } from '../../../../core/auth/auth-service';
import { Router } from '@angular/router';

@Component({
  imports: [
    CurrencyPipe, DatePipe,
    MatTableModule, MatToolbarModule, MatFormFieldModule, MatInputModule,
    MatButtonModule, MatIconModule, MatProgressBarModule, MatPaginatorModule
  ],
  selector: 'app-product-list',
  styleUrl: './product-list.scss',
  templateUrl: './product-list.html',
})
export class ProductList implements OnInit {

  readonly products = signal<Product[]>([]);
  readonly isLoading = signal(false);
  readonly searchTerm = signal("");
  readonly minStock = signal<number | null>(null);
  readonly maxStock = signal<number | null>(null);
  readonly columns = ['id', 'name', 'price', 'stock', 'updatedAt', 'actions'];

  readonly page = signal(1);
  readonly pageSize = signal(10);
  readonly totalCount = signal(0);


  readonly isSearchActive = computed(() => this.searchTerm().trim().length > 0);
  readonly isStockRangeActive = computed(() => this.minStock() !== null && this.maxStock() !== null);
  readonly isPartialStockRange = computed(() => (this.minStock() !== null) !== (this.maxStock() !== null));


  readonly isPaginated = signal(false);

  private readonly authService = inject(AuthService);

  // A signal, so the toolbar reacts to sign-in and sign-out.
  readonly user = this.authService.currentUser;

  constructor(
    private readonly productService: ProductService,
    private readonly dialog: MatDialog,
    private readonly snackBar: MatSnackBar,
    private readonly router: Router,
  ) {
  }

  ngOnInit(): void {
    this.loadProducts();
  }

  signOut(): void {
    this.authService.logout().subscribe(() => {
      this.router.navigate(['/login']);
    });
  }

  loadProducts(): void {
    const term = this.searchTerm().trim();
    const min = this.minStock();
    const max = this.maxStock();
    const page = this.page();
    const pageSize = this.pageSize();

    const isPaginated = term.length > 0 || (min !== null && max !== null);

    const request: Observable<{ items: Product[]; totalCount: number }> = term
      ? this.productService.search(term, page, pageSize)
      : min !== null && max !== null
        ? this.productService.byStockRange(min, max, page, pageSize)
        : this.productService.getAll().pipe(map(items => ({ items, totalCount: items.length })));

    this.isLoading.set(true);
    request.pipe(
      finalize(() => this.isLoading.set(false))
    ).subscribe(result => {
      this.products.set(result.items);
      this.totalCount.set(result.totalCount);
      this.isPaginated.set(isPaginated);
    });
  }

  // Applying a new filter always starts from page 1
  applyFilters(): void {
    this.page.set(1);
    this.loadProducts();
  }

  clearSearchFilters(): void {
    this.searchTerm.set('');
    this.minStock.set(null);
    this.maxStock.set(null);
    this.page.set(1);
    this.loadProducts();
  }

  onPageChange(event: PageEvent): void {
    this.page.set(event.pageIndex + 1);
    this.pageSize.set(event.pageSize);
    this.loadProducts();
  }

  onSearchInput(value: string) {
    this.searchTerm.set(value);
  }

  adjustStock(product: Product, quantity: number) {
    const request = quantity > 0
      ? this.productService.addToStock(product.id, quantity)
      : this.productService.decrementStock(product.id, -quantity);

    request.subscribe({
      next: updatedProduct => {
        this.products.update(list => list.map(p => p.id === updatedProduct.id ? updatedProduct : p));

        this.snackBar.open(
          `${updatedProduct.name} stock updated to ${updatedProduct.stock}`,
          'Dismiss',
          { duration: 3000 }
        );
      },
      error: () => { /* interceptor already surfaced the ProblemDetails detail */ }
    });
  }

  createProduct() {
    this.dialog.open(
      ProductFormDialog, { width: '420px', data: {} })
      .afterClosed()
      .subscribe(created => {
        if (!created) return;
        // A new product sorts to the top by updatedAt, so return to page 1.
        this.page.set(1);
        this.loadProducts();
      });
  }

  editProduct(product: Product) {
    this.dialog.open(ProductFormDialog, { width: '420px', data: { product: product } })
      .afterClosed()
      .subscribe(updated => {
        if (updated) {
          this.products.update(list => list.map(p => p.id === updated.id ? updated : p));
        }
      });
  }

  removeProduct(product: Product) {
    this.dialog.open(ConfirmDialog, {
      data: { title: 'Delete product', message: `Delete "${product.name}"?` }
    })
      .afterClosed()
      .subscribe(confirmed => {
        if (!confirmed) return;
        this.productService.delete(product.id).subscribe(() => {
          this.products.update(list => list.filter(p => p.id !== product.id));
          this.totalCount.update(count => Math.max(0, count - 1));
          this.snackBar.open('Product deleted', 'Dismiss', { duration: 3000 });

          // Deleting the last row of a page would leave it empty.
          if (this.products().length === 0 && this.page() > 1) {
            this.page.update(p => p - 1);
            this.loadProducts();
          }
        });
      });
  }
}