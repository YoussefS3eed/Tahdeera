import { TestBed } from '@angular/core/testing';
import { ReportExporterService } from './report-exporter.service';

describe('ReportExporterService', () => {
  let service: ReportExporterService;

  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [ReportExporterService]
    });
    service = TestBed.inject(ReportExporterService);
  });

  it('should generate CSV with UTF-8 BOM prefix', () => {
    const headers = ['اسم الطالب', 'كود الطالب', 'المبلغ'];
    const rows = [
      ['أحمد محمود', 'ST-1001', 450],
      ['سارة إبراهيم', 'ST-1002', 450]
    ];

    const csv = service.generateCsvContent(headers, rows);
    expect(csv.startsWith('\uFEFF')).toBeTrue();
    expect(csv).toContain('اسم الطالب,كود الطالب,المبلغ');
    expect(csv).toContain('أحمد محمود,ST-1001,450');
  });

  it('should escape quotes and commas in CSV cells', () => {
    const headers = ['الملاحظات'];
    const rows = [
      ['ملاحظة, تحتوي على فاصلة'],
      ['ملاحظة بها "علامات تنصيص" مزدوجة']
    ];

    const csv = service.generateCsvContent(headers, rows);
    expect(csv).toContain('"ملاحظة, تحتوي على فاصلة"');
    expect(csv).toContain('"ملاحظة بها ""علامات تنصيص"" مزدوجة"');
  });

  it('should call triggerDownload on exportToCsv', () => {
    spyOn(service, 'triggerDownload');
    service.exportToCsv('test_report', ['H1'], [['R1']]);
    expect(service.triggerDownload).toHaveBeenCalled();
  });
});
