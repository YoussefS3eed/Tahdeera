import { Injectable } from '@angular/core';

@Injectable({ providedIn: 'root' })
export class ReportExporterService {
  /**
   * Generates a CSV string with a UTF-8 BOM (\uFEFF) to guarantee
   * accurate Arabic character rendering in Microsoft Excel.
   */
  generateCsvContent(headers: string[], rows: (string | number)[][]): string {
    const BOM = '\uFEFF';
    const escapeCell = (val: string | number): string => {
      const str = String(val ?? '');
      if (str.includes(',') || str.includes('"') || str.includes('\n')) {
        return `"${str.replace(/"/g, '""')}"`;
      }
      return str;
    };

    const headerLine = headers.map(escapeCell).join(',');
    const rowLines = rows.map(row => row.map(escapeCell).join(','));

    return BOM + [headerLine, ...rowLines].join('\r\n');
  }

  /**
   * Triggers a browser download of the CSV content.
   */
  exportToCsv(filename: string, headers: string[], rows: (string | number)[][]): void {
    const csvContent = this.generateCsvContent(headers, rows);
    const blob = new Blob([csvContent], { type: 'text/csv;charset=utf-8;' });
    this.triggerDownload(blob, filename.endsWith('.csv') ? filename : `${filename}.csv`);
  }

  triggerDownload(blob: Blob, filename: string): void {
    const url = URL.createObjectURL(blob);
    const link = document.createElement('a');
    link.href = url;
    link.download = filename;
    document.body.appendChild(link);
    link.click();
    document.body.removeChild(link);
    URL.revokeObjectURL(url);
  }

  /**
   * Formats and opens a clean print-ready layout for the given report data.
   */
  printReport(reportTitle: string, headers: string[], rows: (string | number)[][], subtitle?: string): void {
    const printWindow = window.open('', '_blank', 'width=900,height=700');
    if (!printWindow) return;

    const tableHeaders = headers.map(h => `<th style="border: 1px solid #cbd5e1; padding: 8px; background: #f8fafc; font-weight: 600;">${h}</th>`).join('');
    const tableRows = rows.map(r =>
      `<tr>${r.map(cell => `<td style="border: 1px solid #cbd5e1; padding: 8px;">${cell}</td>`).join('')}</tr>`
    ).join('');

    const html = `
      <!DOCTYPE html>
      <html lang="ar" dir="rtl">
      <head>
        <meta charset="utf-8">
        <title>${reportTitle}</title>
        <style>
          body { font-family: -apple-system, BlinkMacSystemFont, "Segoe UI", Roboto, sans-serif; padding: 24px; color: #1e293b; direction: rtl; }
          h1 { margin-bottom: 4px; font-size: 20px; }
          p { margin-top: 0; color: #64748b; font-size: 14px; }
          table { width: 100%; border-collapse: collapse; margin-top: 16px; font-size: 13px; }
          @media print {
            body { padding: 0; }
            @page { margin: 1.5cm; }
          }
        </style>
      </head>
      <body>
        <h1>${reportTitle}</h1>
        ${subtitle ? `<p>${subtitle}</p>` : ''}
        <table>
          <thead><tr>${tableHeaders}</tr></thead>
          <tbody>${tableRows}</tbody>
        </table>
        <script>
          window.onload = function() {
            window.print();
          };
        </script>
      </body>
      </html>
    `;

    printWindow.document.write(html);
    printWindow.document.close();
  }
}
