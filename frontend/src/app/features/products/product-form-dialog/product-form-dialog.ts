import { Component, Inject } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { ProductService } from '../product.service';
import { MAT_DIALOG_DATA, MatDialogModule, MatDialogRef } from '@angular/material/dialog';
import { Product } from '../product.models';
import { finalize } from 'rxjs';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatButtonModule } from '@angular/material/button';

@Component({
  imports: [ReactiveFormsModule, MatDialogModule, MatFormFieldModule, MatInputModule, MatButtonModule],
  selector: 'app-product-form-dialog',
  styleUrl: './product-form-dialog.scss',
  templateUrl: './product-form-dialog.html',
})
export class ProductFormDialog {

  readonly product: Product | undefined;
  readonly isEditMode: boolean;
  isSaving = false;

  private readonly fb: FormBuilder = new FormBuilder();

  readonly form = this.fb.nonNullable.group({
    name: ['', [Validators.required, Validators.minLength(2), Validators.maxLength(100)]],
    price: [0, [Validators.required, Validators.min(0)]],
    stock: [0, [Validators.min(0), Validators.max(1000)]]
  });

  constructor(
    private readonly productService: ProductService,
    private readonly dialogRef: MatDialogRef<ProductFormDialog>,
    @Inject(MAT_DIALOG_DATA) data: { product?: Product },
  ) {

    this.product = data?.product;
    this.isEditMode = this.product != null;

    if (this.product) {
      this.form.controls.stock.disable();
      this.form.patchValue({
        name: this.product.name,
        price: this.product.price
      });
    }
  }

  submit(): void {
    if (this.form.invalid) {
      this.form.markAllAsTouched();
      return;
    }

    const { name, price, stock } = this.form.getRawValue();
    this.isSaving = true;

    const request = this.product
      ? this.productService.update(this.product.id, { name, price })
      : this.productService.create({ name, price, stock });

    request.pipe(finalize(() => { this.isSaving = false; }))
      .subscribe(product => this.dialogRef.close(product));
  }

  cancel(): void {
    this.dialogRef.close(null);
  }
}
