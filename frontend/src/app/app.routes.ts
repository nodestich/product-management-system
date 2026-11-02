import { Routes } from '@angular/router';
import { authGuard } from '../core/auth/auth-guard';

export const routes: Routes = [
    {
        path: 'login',
        loadComponent: () => import ('./features/auth/login/login').then(m => m.Login)
    },
    {
        path: '',
        canActivate: [authGuard],
        loadComponent: () => import('./features/products/product-list/product-list').then(m => m.ProductList)
    },
    { path: '**', redirectTo: '' }
];
