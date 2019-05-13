import { Component, OnInit } from '@angular/core';
import {
  InventoryClient,
  Company,
  ServantInventoryItem,
  ServantAllocation,
  MaterialInventoryItem,
  Material,
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
    inventoryList: ServantInventoryItem[];
    filteredInventoryList: ServantInventoryItem[];
    constructor(private inventoryClient: InventoryClient) { }

    async ngOnInit(): Promise<void> {
        await this.inventoryClient.getCompanies().subscribe(result => {
          this.companies = result;
          this.displayCompanyNames = this.companies.map(c => c.name);
          this.displayCompanyNames.push('Alle');
        });
      }

      public async loadInventory() {
        const tempList = this.selectedCompany === 'Alle' ?
        await this.inventoryClient.getServantInventoryForAll().toPromise() :
        await this.inventoryClient.getServantInventory(this.selectedCompany).toPromise();

        /*this.inventoryList = tempList
          .sort((n1, n2) => {
            if (n1.shortDescription > n2.shortDescription) {
              return 1;
            }

            if (n1.shortDescription < n2.shortDescription) {
              return -1;
            }

            return 0;
          });*/
      }
      public getServantInventoryListByCompany(company: String): ServantInventoryItem[] {
        this.filteredInventoryList = this.inventoryList.filter(item => item.company === company);
        return this.filteredInventoryList;
      }

}
