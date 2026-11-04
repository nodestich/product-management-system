import { signal } from '@angular/core';
import { TestBed } from '@angular/core/testing';
import { provideRouter } from '@angular/router';
import { MatDialog } from '@angular/material/dialog';
import { MatSnackBar } from '@angular/material/snack-bar';
import { of } from 'rxjs';

import { ProductList } from './product-list';
import { ProductService } from '../product.service';
import { PagedResult, Product } from '../product.models';
import { AuthService } from '../../../../core/auth/auth-service';
import { ConfirmDialog } from '../../shared/confirm-dialog/confirm-dialog';

const products: Product[] = [
  {
    id: 100000,
    name: 'Monitor 32 inches',
    price: 249.99,
    stock: 12,
    createdAt: '2026-01-01T00:00:00Z',
    updatedAt: '2026-01-01T00:00:00Z'
  }
];

const searchPage: PagedResult<Product> = {
  items: [products[0], { ...products[0], id: 100001, name: 'Mechanical keyboard' }],
  page: 1,
  pageSize: 10,
  totalCount: 12,
  totalPages: 2,
  hasPrevious: false,
  hasNext: true
};

describe('ProductList', () => {
  let service: {
    getAll: ReturnType<typeof vi.fn>;
    search: ReturnType<typeof vi.fn>;
    byStockRange: ReturnType<typeof vi.fn>;
    addToStock: ReturnType<typeof vi.fn>;
    decrementStock: ReturnType<typeof vi.fn>;
    delete: ReturnType<typeof vi.fn>;
  };
  let dialogResult: Product | null;
  let confirmResult: boolean;

  beforeEach(async () => {
    dialogResult = null;
    confirmResult = false;

    service = {
      getAll: vi.fn(() => of(products)),
      search: vi.fn(() => of(searchPage)),
      byStockRange: vi.fn(() => of(searchPage)),
      addToStock: vi.fn((_id: number, _qty: number) => of({ ...products[0], stock: 13 })),
      decrementStock: vi.fn((_id: number, _qty: number) => of({ ...products[0], stock: 11 })),
      delete: vi.fn(() => of(void 0))
    };

    await TestBed.configureTestingModule({
      imports: [ProductList],
      providers: [
        provideRouter([]),
        { provide: ProductService, useValue: service },
        {
          provide: MatDialog,
          useValue: {
            open: (component: unknown) => ({
              afterClosed: () => of(component === ConfirmDialog ? confirmResult : dialogResult)
            })
          }
        },
        { provide: MatSnackBar, useValue: { open: () => undefined } },
        {
          provide: AuthService,
          useValue: {
            currentUser: signal({ id: 'user-id', email: 'admin@example.com' }),
            isAuthenticated: () => true,
            logout: () => of(void 0)
          }
        }
      ]
    }).compileComponents();
  });

  it('loads products on init and renders one row per product', () => {
    const fixture = TestBed.createComponent(ProductList);
    fixture.detectChanges();

    expect(fixture.componentInstance.products().length).toBe(1);

    const rows = (fixture.nativeElement as HTMLElement).querySelectorAll('tr[mat-row]');
    expect(rows.length).toBe(1);
  });

  it('shows the signed-in user in the toolbar', () => {
    const fixture = TestBed.createComponent(ProductList);
    fixture.detectChanges();

    const text = (fixture.nativeElement as HTMLElement).textContent ?? '';
    expect(text).toContain('admin@example.com');
  });

  it('hides the paginator for the unpaginated list', () => {
    const fixture = TestBed.createComponent(ProductList);
    fixture.detectChanges();

    const paginator = (fixture.nativeElement as HTMLElement).querySelector('mat-paginator');
    expect(paginator).toBeNull();
  });

  it('takes the total count from the paged response and shows the paginator', () => {
    const fixture = TestBed.createComponent(ProductList);
    fixture.detectChanges();
    const component = fixture.componentInstance;

    component.searchTerm.set('Item');
    component.applyFilters();
    fixture.detectChanges();

    expect(component.products().length).toBe(2);
    expect(component.totalCount()).toBe(12);
    expect((fixture.nativeElement as HTMLElement).querySelector('mat-paginator')).toBeTruthy();
  });

  it('returns to page 1 when the filters are cleared', () => {
    const fixture = TestBed.createComponent(ProductList);
    fixture.detectChanges();
    const component = fixture.componentInstance;

    component.searchTerm.set('Item');
    component.applyFilters();
    component.page.set(2);

    component.clearSearchFilters();

    expect(component.page()).toBe(1);
    expect(component.products().length).toBe(1);
  });

  it('treats 0 as a real Min stock bound instead of an empty field', () => {
    const fixture = TestBed.createComponent(ProductList);
    fixture.detectChanges();
    const component = fixture.componentInstance;

    const minInput = (fixture.nativeElement as HTMLElement)
      .querySelectorAll<HTMLInputElement>('input[type=number]')[0];

    minInput.value = '0';
    minInput.dispatchEvent(new Event('input'));
    fixture.detectChanges();

    expect(component.minStock()).toBe(0);

    component.maxStock.set(5);
    component.applyFilters();

    expect(service.byStockRange).toHaveBeenCalledWith(0, 5, 1, 10);
  });

  it('clears the Min stock bound when the field is emptied', () => {
    const fixture = TestBed.createComponent(ProductList);
    fixture.detectChanges();
    const component = fixture.componentInstance;

    const minInput = (fixture.nativeElement as HTMLElement)
      .querySelectorAll<HTMLInputElement>('input[type=number]')[0];

    minInput.value = '';
    minInput.dispatchEvent(new Event('input'));
    fixture.detectChanges();

    expect(component.minStock()).toBeNull();
  });

  it('re-queries the applied stock range after a stock change', () => {
    const fixture = TestBed.createComponent(ProductList);
    fixture.detectChanges();
    const component = fixture.componentInstance;

    component.minStock.set(1);
    component.maxStock.set(5);
    component.applyFilters();

    expect(service.byStockRange).toHaveBeenCalledTimes(1);

    component.adjustStock(products[0], 1);

    expect(service.byStockRange).toHaveBeenCalledTimes(2);
    expect(service.byStockRange).toHaveBeenLastCalledWith(1, 5, 1, 10);
  });

  it('re-queries the applied search after a rename', () => {
    const fixture = TestBed.createComponent(ProductList);
    fixture.detectChanges();
    const component = fixture.componentInstance;

    component.searchTerm.set('Monitor');
    component.applyFilters();

    expect(service.search).toHaveBeenCalledTimes(1);

    dialogResult = { ...products[0], name: 'Keyboard' };
    component.editProduct(products[0]);

    expect(service.search).toHaveBeenCalledTimes(2);
  });

  it('patches the row in place and issues no request when no filter is applied', () => {
    const fixture = TestBed.createComponent(ProductList);
    fixture.detectChanges();
    const component = fixture.componentInstance;

    expect(component.products()[0].stock).toBe(12);

    component.adjustStock(products[0], 1);

    expect(component.products()[0].stock).toBe(13);
    expect(service.getAll).toHaveBeenCalledTimes(1);
    expect(service.byStockRange).not.toHaveBeenCalled();
    expect(service.search).not.toHaveBeenCalled();
  });

  // The mutation handler must re-run the query that produced the visible rows, not
  // re-read the inputs: those hold edits the user typed but never applied.
  it('re-queries with the applied term, not the current input value', () => {
    const fixture = TestBed.createComponent(ProductList);
    fixture.detectChanges();
    const component = fixture.componentInstance;

    component.searchTerm.set('Monitor');
    component.applyFilters();

    expect(service.search).toHaveBeenCalledTimes(1);

    component.onSearchInput('Keyboard');

    component.adjustStock(products[0], 1);

    expect(service.search).toHaveBeenCalledTimes(2);
    expect(service.search).toHaveBeenLastCalledWith('Monitor', 1, 10);
  });

  it('re-queries the applied range, not a search term typed but never applied', () => {
    const fixture = TestBed.createComponent(ProductList);
    fixture.detectChanges();
    const component = fixture.componentInstance;

    component.minStock.set(1);
    component.maxStock.set(5);
    component.applyFilters();

    expect(service.byStockRange).toHaveBeenCalledTimes(1);

    component.onSearchInput('mon');

    component.adjustStock(products[0], 1);

    expect(service.byStockRange).toHaveBeenCalledTimes(2);
    expect(service.search).not.toHaveBeenCalled();
  });

  it('re-queries the applied filter after a delete', () => {
    const fixture = TestBed.createComponent(ProductList);
    fixture.detectChanges();
    const component = fixture.componentInstance;

    component.searchTerm.set('Monitor');
    component.applyFilters();

    expect(service.search).toHaveBeenCalledTimes(1);

    confirmResult = true;
    component.removeProduct(products[0]);

    expect(service.delete).toHaveBeenCalledWith(products[0].id);
    expect(service.search).toHaveBeenCalledTimes(2);
  });

  it('returns to page 1 and re-queries after a product is created', () => {
    const fixture = TestBed.createComponent(ProductList);
    fixture.detectChanges();
    const component = fixture.componentInstance;

    component.searchTerm.set('Monitor');
    component.applyFilters();
    component.page.set(2);

    dialogResult = { ...products[0], id: 100099, name: 'New product' };
    component.createProduct();

    expect(component.page()).toBe(1);
    expect(service.search).toHaveBeenCalledTimes(2);
    expect(service.search).toHaveBeenLastCalledWith('Monitor', 1, 10);
  });
});