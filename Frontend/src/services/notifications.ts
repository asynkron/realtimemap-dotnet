// this is copied from ToastService.d.ts
interface ToastServiceMethods {
  add(message: any): any;
  removeGroup(group: any): void;
  removeAllGroups(): void;
}

interface Window {
  toast: ToastServiceMethods;
}
