import { Component, OnInit, Inject } from '@angular/core';
import { InventoryClient, Company, API_BASE_URL } from '../services/api_client_generated';
import { Router } from '@angular/router';

@Component({
  selector: 'company-list',
  templateUrl: './company-list.component.html'
})
export class CompanyListComponent implements OnInit {

  public companies: Company[];
  constructor(
    public inventoryClient: InventoryClient,
    private router: Router,
     @Inject(API_BASE_URL) public baseUrl?: string
  ) {}

  async ngOnInit() {
    await this.loadCompanies();
    // this.inventoryClient.getCompanies().subscribe(result => {
    //   this.companies = result;
    // });
  }

  private async loadCompanies() {
    this.companies = await this.inventoryClient.getCompanies().toPromise();
  }

  public getLocationList(currentCompany: Company): string {
    return currentCompany.locations
      .map(loc => loc.name)
      .join(', ')
      .toString();
  }

  public async edit(companyId: string) {
    await this.router.navigate(['/edit-company', companyId]);
  }

  public async delete(id: string) {
    await this.inventoryClient.deleteCompanyById(id).toPromise();
    await this.loadCompanies();
    // this.inventoryClient.deleteCompanyById(id).subscribe(() => {
    //   this.inventoryClient.getCompanies().subscribe(result => {
    //     this.companies = result;
    //   });
    // });
  }

  public getDownloadText(company: Company): string {

    if (company.locations.length > 1) {
      return ' Template zu Kp Verteilung';
    } else {
      return ' Template zu Kp Erfassung';
    }
  }

  public getDownloadCompanyTemplateDefaultLocationUrl(companyName): string {
    let url_ = this.baseUrl + '/api/Companies/{companyName}/templates';
    if (companyName === undefined || companyName === null) {
        throw new Error('The parameter \'companyName\' must be defined.');
    }
    url_ = url_.replace('{companyName}', encodeURIComponent('' + companyName));
    url_ = url_.replace(/[?&]$/, '');
    return url_;
  }
}
