import { Component } from '@angular/core';
import {InventoryClient, Report} from '../services/api_client_generated';

@Component({
  selector: 'app-fetch-data',
  templateUrl: './report-list.component.html',
  styleUrls: ['./report-list.component.css']
})
export class ReportListComponent {
  reports: Report[];

  constructor(public inventoryClient: InventoryClient) {
    this.fetchReports();
  }

  private async fetchReports() {
    this.reports = await this.inventoryClient.getReportList().toPromise();
  }

  async generateReport() {
    await this.inventoryClient.generateTechMatReport().toPromise();
    await this.fetchReports();

  }

  async delete(id: string) {
    await this.inventoryClient.deleteReportById(id).toPromise();
    await this.fetchReports();
  }

  public  getReportDownloadLink(id: string): string {
    return `${window.location.origin}/api/Bataillon/reports/material/${id}`;
  }
}
