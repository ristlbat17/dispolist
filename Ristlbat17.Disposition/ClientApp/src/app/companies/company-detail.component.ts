import { Component, OnInit, Input, Output, EventEmitter } from '@angular/core';
import { Router } from '@angular/router';
import {Company, Location, InventoryClient} from '../services/api_client_generated';



@Component({
    selector: 'company-detail',
    templateUrl: 'company-detail.component.html'
})

export class CompanyDetailComponent implements OnInit {
    @Input()
    public company: Company;
    @Input()
    public message: string;
    @Input()
    public editMode: boolean;

    constructor(private router: Router, private inventoryClient: InventoryClient) { }

    ngOnInit() {

     }

     removeLocation(location: Location) {
         this.company.locations = this.company.locations.filter(obj => obj !== location);
     }

     addLocation(name): Location {
        return new Location({ name: name });
    }

    locationRemoved(name: string) {
        // always keep KP Rw!
        if (name === 'KP Rw') {
            this.company.locations.push(new Location({name: 'KP Rw'}));
        }
    }


     async submit() {
         if (this.editMode) {
            await this.inventoryClient.updateCompany(this.company).toPromise();
         } else {
            await this.inventoryClient.newCompany(this.company).toPromise();
         }
         await this.router.navigate(['/company-list']);
     }

     async cancel() {
        await this.router.navigate(['/company-list']);
     }
}
