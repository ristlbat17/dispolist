import { Component, OnInit } from '@angular/core';
import { Company, InventoryClient, Location } from '../services/api_client_generated';

@Component({
    template: `
    <company-detail [message]="'Neue Kompanie erfassen'"
    [company]="newCompany"
    [editMode]="false"></company-detail>
    `
})

export class NewCompanyComponent implements OnInit {
    newCompany: Company;
    constructor() { }

    ngOnInit() {
        this.newCompany = new Company();
        const defaultCompany = new Location({name: 'KP Rw'});
        this.newCompany.defaultLocation = defaultCompany;
        this.newCompany.locations.push(defaultCompany);
    }
}
