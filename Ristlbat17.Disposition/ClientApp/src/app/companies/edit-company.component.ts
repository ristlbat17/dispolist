import { Component, OnInit } from '@angular/core';
import { ActivatedRoute, Router  } from '@angular/router';
import { Company, InventoryClient } from '../services/api_client_generated';

@Component({
    template: `
    <company-detail [message]="'Kompanie bearbeiten'"
    [company]="currentCompany"
    [editMode]="true"></company-detail>
    `
})

export class EditCompanyComponent implements OnInit {
    currentCompany: Company;
    constructor(private inventoryClient: InventoryClient, private activatedRoute: ActivatedRoute, private router: Router) { }

    async ngOnInit() {
        const compId = this.activatedRoute.snapshot.params['id'];
        this.currentCompany = await this.inventoryClient.getCompanyById(compId).toPromise();
    }
}
