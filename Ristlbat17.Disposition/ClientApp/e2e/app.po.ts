import { browser, by, element } from 'protractor';

export class AppPage {
  navigateTo() {
    return browser.get('/');
  }

  async navigateToFetch() {
    await browser.get('/fetch-data');
    return new FetchPage();
  }

  getMainHeading() {
    return element(by.css('app-root h1')).getText();
  }
}

export class FetchPage {
  getMainHeading() {
    return element(by.css('app-root h1')).getText();
  }
}
