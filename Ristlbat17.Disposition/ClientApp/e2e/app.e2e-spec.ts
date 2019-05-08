import { AppPage } from './app.po';

describe('App', () => {
  let page: AppPage;

  beforeEach(() => {
    page = new AppPage();
  });

  it('should display welcome message', () => {
    page.navigateTo();
    expect(page.getMainHeading()).toEqual('Hello, world!');
  });
  it('should navigate to fetch data',
    () => {
      page.navigateToFetch().then((fetchPage) => {
        expect(fetchPage.getMainHeading()).toEqual('Weather forecast');
      });
    });
});
