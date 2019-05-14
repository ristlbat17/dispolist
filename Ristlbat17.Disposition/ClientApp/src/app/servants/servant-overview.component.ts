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

      public async loadServantInventory() {
        const tempList = this.selectedCompany === 'Alle' ?
        await this.inventoryClient.getServantInventoryForAll().toPromise() :
        await this.inventoryClient.getServantInventory(this.selectedCompany).toPromise();

        this.inventoryList = tempList;
      }
      public getServantInventoryListByCompany(company: String): ServantInventoryItem[] {
        if (company === 'Alle') {
          return;
        }

        this.filteredInventoryList = this.inventoryList.filter(item => item.company === company);
        return this.filteredInventoryList;
      }
      public getGradeText(grade: Number) {
          if (grade === 0) {
            return 'Offiziere';
          } else if (grade === 1) {
            return 'Höhere Unteroffiziere';
          } else if (grade === 2) {
            return 'Unteroffiziere';
          } else {
            return 'Mannschaft';
          }
      }
      public getSummedAllocation(iventory: ServantInventoryItem) {
        if (iventory && iventory.distribution) {
          return new ServantAllocation({
            stock: iventory.distribution
              .map(d => d.stock)
              .reduce((a, b) => a + b, 0),
            available: iventory.distribution
              .map(d => d.available)
              .reduce((a, b) => a + b, 0),
            detached: iventory.distribution
              .map(d => d.detached)
              .reduce((a, b) => a + b, 0),
            used: iventory.distribution.map(d => d.used).reduce((a, b) => a + b, 0)
          });
        }
      }


}
