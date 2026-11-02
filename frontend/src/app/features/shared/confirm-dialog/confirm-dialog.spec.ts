import { TestBed } from '@angular/core/testing';
import { MAT_DIALOG_DATA } from '@angular/material/dialog';

import { ConfirmDialog } from './confirm-dialog';

describe('ConfirmDialog', () => {
  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [ConfirmDialog],
      providers: [
        {
          provide: MAT_DIALOG_DATA,
          useValue: { title: 'Delete product', message: 'Delete "Monitor"?' }
        }
      ]
    }).compileComponents();
  });

  it('renders the provided title and message', () => {
    const fixture = TestBed.createComponent(ConfirmDialog);
    fixture.detectChanges();

    const text = (fixture.nativeElement as HTMLElement).textContent ?? '';
    expect(text).toContain('Delete product');
    expect(text).toContain('Delete "Monitor"?');
  });
});