import { Component, OnInit } from '@angular/core';
import { ActivatedRoute, Router  } from '@angular/router';
import {  InventoryClient, Material } from '../services/api_client_generated';

@Component({
    template: `
    <material-detail (cancel)="backToOverview()"
    [editMode]="true" [material]="current" (materialSubmit)="save()" >
    </material-detail>
    `
})

export class EditMaterialComponent implements OnInit {
    current: Material;
    constructor(private inventoryClient: InventoryClient, private activatedRoute: ActivatedRoute, private router: Router) { }

    async ngOnInit() {
        const id = this.activatedRoute.snapshot.params['id'];
        this.current = await this.inventoryClient.getMaterialById(id).toPromise();
    }

    async backToOverview() {
        await this.router.navigate(['material-list']);
    }

    async save() {
        await this.inventoryClient.updateMaterial(
          this.current.id,
          this.current
        ).toPromise();
        await  this.router.navigate(['material-list']);
    }

}
