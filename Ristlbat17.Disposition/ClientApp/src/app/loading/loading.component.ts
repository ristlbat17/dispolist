import { Component } from "@angular/core";

@Component({
    selector: 'loading',
    template : `
    <div class="row text-center centered-loader">
        <i class="fa fa-spinner fa-spin loader"></i>
        <h3>Feldtelefon 96 verbinde ...</h3>
    </div>
    `
})
export class LoadingComponent {
    constructor(){
    }
}