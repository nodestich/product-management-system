import { TestBed } from '@angular/core/testing';
import { MatDialog } from '@angular/material/dialog';
import { MatSnackBar } from '@angular/material/snack-bar';
import { of } from 'rxjs';

import { ProductList } from './product-list';
import { ProductService } from '../product.service';
import { Product } from '../product.models';

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

describe('ProductList', () => {
  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [ProductList],
      providers: [
        { provide: ProductService, useValue: { getAll: () => of(products) } },
        { provide: MatDialog, useValue: { open: () => ({ afterClosed: () => of(null) }) } },
        { provide: MatSnackBar, useValue: { open: () => undefined } }
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
});