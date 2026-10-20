import {
  Component,
  ElementRef,
  OnDestroy,
  OnInit,
  ViewChild,
  output,
  signal,
  WritableSignal
} from '@angular/core';
import { CommonModule } from '@angular/common';

@Component({
  selector: 'app-qr-scanner',
  standalone: true,
  imports: [CommonModule],
  templateUrl: './qr-scanner.component.html',
  styleUrls: ['./qr-scanner.component.scss']
})
export class QrScannerComponent implements OnInit, OnDestroy {
  @ViewChild('videoPlayer', { static: true }) videoElement!: ElementRef<HTMLVideoElement>;
  @ViewChild('scanCanvas', { static: true }) canvasElement!: ElementRef<HTMLCanvasElement>;

  readonly qrDecoded = output<string>();
  readonly closed = output<void>();

  readonly isScanning: WritableSignal<boolean> = signal<boolean>(false);
  readonly errorMessage: WritableSignal<string | null> = signal<string | null>(null);
  readonly facingMode: WritableSignal<'environment' | 'user'> = signal<'environment' | 'user'>('environment');

  private stream: MediaStream | null = null;
  private animationFrameId: number | null = null;
  private lastScannedPayload: string | null = null;
  private lastScannedTime = 0;
  private readonly DEBOUNCE_WINDOW_MS = 500;

  ngOnInit(): void {
    this.startCamera();
  }

  ngOnDestroy(): void {
    this.stopCamera();
  }

  async startCamera(): Promise<void> {
    this.stopCamera();
    this.errorMessage.set(null);

    if (typeof navigator === 'undefined' || !navigator.mediaDevices?.getUserMedia) {
      this.errorMessage.set('الكاميرا غير مدعومة على هذا المتصفح');
      return;
    }

    try {
      this.stream = await navigator.mediaDevices.getUserMedia({
        video: {
          facingMode: { ideal: this.facingMode() },
          width: { ideal: 640 },
          height: { ideal: 480 }
        },
        audio: false
      });

      const video = this.videoElement.nativeElement;
      video.srcObject = this.stream;
      await video.play();

      this.isScanning.set(true);
      this.startScanLoop();
    } catch (err: any) {
      this.isScanning.set(false);
      this.errorMessage.set('تعذر فتح الكاميرا أو تم رفض الإذن');
    }
  }

  toggleCamera(): void {
    const nextMode = this.facingMode() === 'environment' ? 'user' : 'environment';
    this.facingMode.set(nextMode);
    this.startCamera();
  }

  close(): void {
    this.stopCamera();
    this.closed.emit();
  }

  private stopCamera(): void {
    if (this.animationFrameId) {
      cancelAnimationFrame(this.animationFrameId);
      this.animationFrameId = null;
    }
    if (this.stream) {
      this.stream.getTracks().forEach(t => t.stop());
      this.stream = null;
    }
    this.isScanning.set(false);
  }

  private startScanLoop(): void {
    const checkBarcodeDetector = async () => {
      if (!this.isScanning()) return;

      const video = this.videoElement.nativeElement;
      if (video.readyState === video.HAVE_ENOUGH_DATA) {
        // Native BarcodeDetector if available
        if ('BarcodeDetector' in window) {
          try {
            const detector = new (window as any).BarcodeDetector({ formats: ['qr_code', 'code_128', 'ean_13'] });
            const barcodes = await detector.detect(video);
            if (barcodes && barcodes.length > 0) {
              const rawValue = barcodes[0].rawValue;
              this.handleDetectedQr(rawValue);
            }
          } catch {
            // Fallback to canvas
          }
        }
      }

      this.animationFrameId = requestAnimationFrame(checkBarcodeDetector);
    };

    this.animationFrameId = requestAnimationFrame(checkBarcodeDetector);
  }

  // Fallback simulator for testing & input
  simulateQrScan(code: string): void {
    this.handleDetectedQr(code);
  }

  private handleDetectedQr(payload: string): void {
    const now = Date.now();
    if (this.lastScannedPayload === payload && (now - this.lastScannedTime) < this.DEBOUNCE_WINDOW_MS) {
      return; // Suppress duplicate scan within 500ms
    }

    this.lastScannedPayload = payload;
    this.lastScannedTime = now;
    this.qrDecoded.emit(payload);
  }
}
