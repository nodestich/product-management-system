import { TestBed } from '@angular/core/testing';
import { MAT_DIALOG_DATA, MatDialogRef } from '@angular/material/dialog';
import { of } from 'rxjs';

import { ProductFormDialog } from './product-form-dialog';
import { ProductService } from '../product.service';
import { Product } from '../product.models';

const product: Product = {
  id: 100000,
  name: 'Monitor 32 inches',
  price: 249.99,
  stock: 12,
  createdAt: '2026-01-01T00:00:00Z',
  updatedAt: '2026-01-01T00:00:00Z'
};

const productService = {
  create: () => of(product),
  update: () => of(product)
};

const dialogRef = { close: () => undefined };

describe('ProductFormDialog', () => {
  describe('create mode', () => {
    beforeEach(async () => {
      await TestBed.configureTestingModule({
        imports: [ProductFormDialog],
        providers: [
          { provide: ProductService, useValue: productService },
          { provide: MatDialogRef, useValue: dialogRef },
          { provide: MAT_DIALOG_DATA, useValue: {} }
        ]
      }).compileComponents();
    });

    it('is not in edit mode and requires a name', () => {
      const fixture = TestBed.createComponent(ProductFormDialog);
      fixture.detectChanges();
      const component = fixture.componentInstance;

      expect(component.isEditMode).toBe(false);
      expect(component.form.controls.name.hasError('required')).toBe(true);
      expect(component.form.controls.stock.enabled).toBe(true);
    });
  });

  describe('edit mode', () => {
    beforeEach(async () => {
      await TestBed.configureTestingModule({
        imports: [ProductFormDialog],
        providers: [
          { provide: ProductService, useValue: productService },
          { provide: MatDialogRef, useValue: dialogRef },
          { provide: MAT_DIALOG_DATA, useValue: { product } }
        ]
      }).compileComponents();
    });

    it('prefills the form and disables the stock control', () => {
      const fixture = TestBed.createComponent(ProductFormDialog);
      fixture.detectChanges();
      const component = fixture.componentInstance;

      expect(component.isEditMode).toBe(true);
      expect(component.form.controls.name.value).toBe('Monitor 32 inches');
      expect(component.form.controls.stock.disabled).toBe(true);
    });
  });
});