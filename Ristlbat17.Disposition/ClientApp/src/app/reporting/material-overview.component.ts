import { Component, OnInit } from '@angular/core';
import {
  InventoryClient,
  Company,
  MaterialInventoryItem,
  Material,
  MaterialAllocation,
  IMaterialInventoryItem
} from '../services/api_client_generated';

export class EnhancedMaterialInventoryItem extends MaterialInventoryItem {
  public shortDescription: string;
  constructor(data?: IEnhancedMaterialInventoryItem) {
    super(data);
    if (data) {
      for (const property in data) {
        if (data.hasOwnProperty(property)) {
          (<any>this)[property] = (<any>data)[property];
        }
      }
    }
  }
}

export interface IEnhancedMaterialInventoryItem extends IMaterialInventoryItem {
  shortDescription: string;
}

@Component({
  templateUrl: './material-overview.component.html'
})
export class MaterialOverviewComponent implements OnInit {

  companies: Company[];
  displayCompanyNames: string[];
  selectedCompany: string;
  inventoryList: EnhancedMaterialInventoryItem[];
  filteredInventoryList: EnhancedMaterialInventoryItem[];
  material: Material[];
  constructor(private inventoryClient: InventoryClient) { }

  async ngOnInit(): Promise<void> {
    await this.fetchMaterial();

    await this.inventoryClient.getCompanies().subscribe(result => {
      this.companies = result;
      this.displayCompanyNames = this.companies.map(c => c.name);
      this.displayCompanyNames.push('Alle');
    });
  }

  private async fetchMaterial() {
    await this.inventoryClient.getMaterialList().subscribe(result => {
      this.material = result.sort((n1, n2) => {
        if (n1.shortDescription > n2.shortDescription) {
          return 1;
        }

        if (n1.shortDescription < n2.shortDescription) {
          return -1;
        }
        return 0;
      });
    });
  }

  public async loadInventory() {
    const tempList = this.selectedCompany === 'Alle' ?
      await this.inventoryClient.getMaterialInventoryForAll().toPromise() :
      await this.inventoryClient.getMaterialInventory(this.selectedCompany).toPromise();

    this.inventoryList = tempList
      .map(i => this.getEnhancedInventoryItem(i))
      .sort((n1, n2) => {
        if (n1.shortDescription > n2.shortDescription) {
          return 1;
        }

        if (n1.shortDescription < n2.shortDescription) {
          return -1;
        }

        return 0;
      });
  }

  private getEnhancedInventoryItem(inventoryItem: MaterialInventoryItem): EnhancedMaterialInventoryItem {
    const material = this.material.find(m => m.sapNr === inventoryItem.sapNr);
    return new EnhancedMaterialInventoryItem({
      damaged: inventoryItem.damaged,
      available: inventoryItem.available,
      stock: inventoryItem.stock,
      used: inventoryItem.used,
      sapNr: inventoryItem.sapNr,
      shortDescription: material ? material.shortDescription : 'gelöschtes material',
      distribution: inventoryItem.distribution,
      company: inventoryItem.company
    });
  }

  public getInventoryListByMaterial(sapNr: String): MaterialInventoryItem[] {
    this.filteredInventoryList = this.inventoryList.filter(item => item.sapNr === sapNr);
    return this.filteredInventoryList;

  }
  public getSummedAllocation(iventory: MaterialInventoryItem) {
    if (iventory && iventory.distribution) {
      return new MaterialAllocation({
        stock: iventory.distribution
          .map(d => d.stock)
          .reduce((a, b) => a + b, 0),
        available: iventory.distribution
          .map(d => d.available)
          .reduce((a, b) => a + b, 0),
        damaged: iventory.distribution
          .map(d => d.damaged)
          .reduce((a, b) => a + b, 0),
        used: iventory.distribution.map(d => d.used).reduce((a, b) => a + b, 0)
      });
    }
  }
}
