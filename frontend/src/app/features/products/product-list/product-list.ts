import { Component, OnInit, computed, signal } from '@angular/core';
import { ProductService } from '../product.service';
import { Product } from '../product.models';
import { MatDialog } from '@angular/material/dialog';
import { finalize } from 'rxjs';
import { ProductFormDialog } from '../product-form-dialog/product-form-dialog';
import { CurrencyPipe, DatePipe } from '@angular/common';
import { MatTableModule } from '@angular/material/table';
import { MatToolbarModule } from '@angular/material/toolbar';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatProgressBarModule } from '@angular/material/progress-bar';
import { MatIconModule } from '@angular/material/icon';
import { MatButtonModule } from '@angular/material/button';
import { ConfirmDialog } from '../../shared/confirm-dialog/confirm-dialog';
import { MatSnackBar } from '@angular/material/snack-bar';

@Component({
  imports: [
    CurrencyPipe, DatePipe,
    MatTableModule, MatToolbarModule, MatFormFieldModule, MatInputModule,
    MatButtonModule, MatIconModule, MatProgressBarModule
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

  // The API accepts either a name search or a stock range, not both, so the UI
  // makes the two filter groups mutually exclusive. The range only applies when
  // both bounds are set, which mirrors loadProducts().
  readonly isSearchActive = computed(() => this.searchTerm().trim().length > 0);
  readonly isStockRangeActive = computed(() => this.minStock() !== null && this.maxStock() !== null);
  readonly isPartialStockRange = computed(() => (this.minStock() !== null) !== (this.maxStock() !== null));

  constructor(
    private readonly productService: ProductService,
    private readonly dialog: MatDialog,
    private readonly snackBar: MatSnackBar
  ) {

  }


  ngOnInit(): void {
    this.loadProducts();
  }

  loadProducts() {
    const term = this.searchTerm().trim();
    const min = this.minStock();
    const max = this.maxStock();

    // Search if there is a search term, if not try by stock range, else get all
    const request = term
      ? this.productService.search(term)
      : min !== null && max !== null
        ? this.productService.byStockRange(min, max)
        : this.productService.getAll();

    this.isLoading.set(true);
    request.pipe(
      finalize(() => this.isLoading.set(false))
    ).subscribe(data => this.products.set(data));
  }


  clearSearchFilters() {
    this.searchTerm.set('');
    this.minStock.set(null);
    this.maxStock.set(null);
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
      .subscribe(created => { if (created) this.loadProducts(); });
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
          this.snackBar.open('Product deleted', 'Dismiss', { duration: 3000 });
        });
      });
  }
}