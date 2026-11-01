import { Routes } from '@angular/router';

export const routes: Routes = [
    {
        path: '',
        loadComponent: () => import('./features/products/product-list/product-list').then(m => m.ProductList)
    },
    { path: '**', redirectTo: '' }
];
