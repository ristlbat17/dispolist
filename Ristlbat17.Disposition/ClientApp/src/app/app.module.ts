import { BrowserModule } from '@angular/platform-browser';

import { NgModule } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { NgbModule } from '@ng-bootstrap/ng-bootstrap';
import { HttpClientModule } from '@angular/common/http';
import { RouterModule } from '@angular/router';
import { NgSelectModule } from '@ng-select/ng-select';

// TODO create barrel for all imports + modules

import { AppComponent } from './app.component';
import { NavMenuComponent } from './nav-menu/nav-menu.component';
import { MaterialOverviewComponent } from './reporting/material-overview.component';
import { ReportListComponent } from './material/report-list.component';
import { InventoryClient, API_BASE_URL } from './services/api_client_generated';
import { CompanyListComponent } from './companies/company-list.component';
import { NewCompanyComponent } from './companies/new-company.component';
import { CompanyDetailComponent } from './companies/company-detail.component';
import { LoadingComponent } from './loading/loading.component';
import { Ng2IziToastModule } from 'ng2-izitoast';
import { EditCompanyComponent } from './companies/edit-company.component';
import { MaterialListComponent } from './material/material-list.component';
import { MaterialDetailComponent } from './material/material-detail.component';
import { FileUploadComponent } from './upload/file-upload.component';
import { NewMaterialComponent } from './material/new-material.component';
import { EditMaterialComponent } from './material/edit-material.component';
import { DamagedComponent } from './inventory/damaged.component';
import { RepairedComponent } from './inventory/repaired.component';
import { MaterialAllocationComponent } from './inventory/allocation.component';
import { HelpComponent } from './help/help.component';
import { CorrectStockComponent } from './inventory/correct-stock.component';
import { InventoryChangeComponent } from './inventory/inventory-change.component';
import { MaterialUsedComponent } from './inventory/material-used.component';
import { ReportServantsChangesComponent } from './servants/report-servants.component';
import { ServantDetachedComponent } from './servants/detached.component';
import { ServantAvailableComponent } from './servants/available.component';
import { ServantUsedComponent } from './servants/used.component';
import { ServantStockCorrectionComponent } from './servants/stock-correction.component';
import { ReportMaterialChangesComponent } from './inventory/report-material.component';
import { ServantChangeComponent } from './servants/servant-change.component';
import { ServantIdealCorrectionComponent } from './servants/ideal-correction.component';
import { ServantAllocationComponent } from './servants/allocation.component';
import { ServantOverviewComponent } from './servants/servant-overview.component';

@NgModule({
  declarations: [
    AppComponent,
    NavMenuComponent,
    MaterialOverviewComponent,
    HelpComponent,
    ReportListComponent,
    CompanyListComponent,
    NewCompanyComponent,
    CompanyDetailComponent,
    EditCompanyComponent,
    LoadingComponent,
    MaterialListComponent,
    MaterialDetailComponent,
    FileUploadComponent,
    NewMaterialComponent,
    EditMaterialComponent,
    DamagedComponent,
    RepairedComponent,
    MaterialAllocationComponent,
    CorrectStockComponent,
    InventoryChangeComponent,
    MaterialUsedComponent,
    ReportServantsChangesComponent,
    ServantDetachedComponent,
    ServantAvailableComponent,
    ServantUsedComponent,
    ServantStockCorrectionComponent,
    ReportMaterialChangesComponent,
    ServantChangeComponent,
    ServantIdealCorrectionComponent,
    ServantAllocationComponent,
    ServantOverviewComponent
  ],
  imports: [
    BrowserModule.withServerTransition({ appId: 'ng-cli-universal' }),
    HttpClientModule,
    FormsModule,
    NgSelectModule,
    Ng2IziToastModule,
    NgbModule.forRoot(),
    RouterModule.forRoot([
      { path: '', component: MaterialOverviewComponent, pathMatch: 'full' },
      { path: 'help', component: HelpComponent },
      { path: 'servant-overview', component: ServantOverviewComponent },
      { path: 'report-list', component: ReportListComponent },
      { path: 'company-list', component: CompanyListComponent },
      { path: 'new-company', component: NewCompanyComponent },
      { path: 'edit-company/:id', component: EditCompanyComponent },
      { path: 'material-list', component: MaterialListComponent },
      { path: 'new-material', component: NewMaterialComponent },
      { path: 'edit-material/:id', component: EditMaterialComponent },
      { path: 'upload', component: FileUploadComponent },
      {
        path: 'report-servants',
        component: ReportServantsChangesComponent,
        pathMatch: 'full'
      },
      {
        path: 'report-material',
        component: ReportMaterialChangesComponent,
        pathMatch: 'full'
      }
    ])
  ],
  providers: [
    InventoryClient,
    { provide: API_BASE_URL, useValue: window.location.origin }
  ],
  bootstrap: [AppComponent]
})
export class AppModule { }
