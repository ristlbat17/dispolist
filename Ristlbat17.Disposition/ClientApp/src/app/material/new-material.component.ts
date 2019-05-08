import { Component, OnInit } from '@angular/core';
import { Router } from '@angular/router';
import { InventoryClient, Material } from '../services/api_client_generated';

@Component({
  template: `
    <material-detail (cancel)="backToOverview()"
    [editMode]="false" [material]="current" (materialSubmit)="save()" >
    </material-detail>
    `
})
export class NewMaterialComponent implements OnInit {
  current: Material;
  constructor(
    private inventoryClient: InventoryClient,
    private router: Router
  ) { }

  ngOnInit() {
    this.current = new Material();
  }
  async backToOverview() {
    await this.router.navigate(['material-list']);
  }

  async save() {
    await this.inventoryClient.newMaterial(this.current).toPromise();
    await this.router.navigate(['material-list']);
  }
}
