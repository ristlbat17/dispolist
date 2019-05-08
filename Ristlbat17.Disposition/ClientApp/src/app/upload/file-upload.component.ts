import { Component, OnInit } from "@angular/core";
import { HttpClient, HttpRequest, HttpEventType, HttpErrorResponse } from "@angular/common/http";
import { InventoryClient, Company } from "../services/api_client_generated";
import { Ng2IzitoastService } from "ng2-izitoast";

@Component({
  selector: "file-upload",
  templateUrl: "./file-upload.component.html"
})
export class FileUploadComponent implements OnInit {
  companies: Company[];
  ngOnInit(): void {
    this.inventoryClient
      .getCompanies()
      .subscribe(result => (this.companies = result));
  }
  public progress: number;
  public message: string;
  public selectedCompany: Company;
  public errorMessage : string;

  constructor(
    private http: HttpClient,
    private inventoryClient: InventoryClient,
    private iziService: Ng2IzitoastService
  ) {}

  upload(files) {
    if (files.length === 0) return;

    const formData = new FormData();

    for (let file of files)
      formData.append("initialReportPerCompany", file, file.name);

    const uploadReq = new HttpRequest(
      "POST",
      "/api/companies/"+this.selectedCompany.name+"/report",
      formData,
      {
        reportProgress: true
      }
    );

    this.http.request(uploadReq).subscribe(event => {
      if (event.type === HttpEventType.UploadProgress)
        this.progress = Math.round((100 * event.loaded) / event.total);
      else if (event.type === HttpEventType.Response){
        this.iziService.success({message: "Meldung erfolgreich verabeitet"});
      }
       
    },
      (error: HttpErrorResponse ) =>{
        this.errorMessage = JSON.stringify(error.error);
      }
    );
  }
}
