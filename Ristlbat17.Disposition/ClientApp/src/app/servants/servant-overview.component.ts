import { Component, OnInit } from '@angular/core';
import {
  InventoryClient,
  Company,
  MaterialInventoryItem,
  Material,
  MaterialAllocation,
  IMaterialInventoryItem
} from '../services/api_client_generated';

@Component({
    selector: 'servants-overview',
    templateUrl: './servant-overview.component.html'
})
export class ServantOverviewComponent implements OnInit {

    companies: Company[];
    displayCompanyNames: string[];
    selectedCompany: string;
    constructor(private inventoryClient: InventoryClient) { }

    async ngOnInit(): Promise<void> {
        await this.inventoryClient.getCompanies().subscribe(result => {
          this.companies = result;
          this.displayCompanyNames = this.companies.map(c => c.name);
          this.displayCompanyNames.push('Alle');
        });
      }

}
