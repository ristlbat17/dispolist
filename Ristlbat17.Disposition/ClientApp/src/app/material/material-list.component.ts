import { Component, OnInit } from '@angular/core';
import { InventoryClient, Material } from '../services/api_client_generated';
import { Router } from '@angular/router';

@Component({
  selector: 'material-list',
  templateUrl: 'material-list.component.html'
})
export class MaterialListComponent implements OnInit {
  materialList: Material[];
  current: Material;
  showDetail: boolean;
  editMode: boolean;
  constructor(private inventoryClient: InventoryClient, private router: Router) { }

  ngOnInit() {
    this.fetchMaterial();
  }

  private async fetchMaterial() {
    await this.inventoryClient.getMaterialList().subscribe(result => {
      this.materialList = result;
    });
  }

  async newMaterial() {
    await this.router.navigate(['new-material']);
  }

  async delete(id: string) {
    await this.inventoryClient.deleteMaterialById(id).subscribe(() => this.fetchMaterial());
  }
}
