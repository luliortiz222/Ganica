import type { CapacitorConfig } from '@capacitor/cli';

const config: CapacitorConfig = {
  appId: 'io.ionic.starter',
  appName: 'Gánica',
  webDir: 'dist',
  server: {
    cleartext: true,
    androidScheme: 'http'
  },
  plugins: {
    CapacitorHttp: {
      enabled: false
    }
  }
};

export default config;