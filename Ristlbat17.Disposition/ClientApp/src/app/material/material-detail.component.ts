import { Component, OnInit, Input, Output, EventEmitter } from '@angular/core';
import { Material, InventoryClient } from '../services/api_client_generated';

@Component({
    selector: 'material-detail',
    templateUrl: 'material-detail.component.html'
})

export class MaterialDetailComponent implements OnInit {
    @Input()
    public material: Material;

    @Input()
    public editMode: boolean;

    @Output()
    public cancel = new EventEmitter();

    @Output()
    public materialSubmit = new EventEmitter();

    categories: string[];

    constructor(private inventoryClient: InventoryClient) { }

    async ngOnInit() {
        const materialList = await this.inventoryClient.getMaterialList().toPromise();
        this.categories = materialList.map(mat => mat.category).filter(this.onlyUnique);
    }

    onlyUnique(value, index, self) {
        return self.indexOf(value) === index;
    }


    submitClick() {
        this.materialSubmit.emit();
    }

    cancelClick() {
        this.cancel.emit();
    }
}
