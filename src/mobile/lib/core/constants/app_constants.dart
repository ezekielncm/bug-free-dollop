class AppConstants {
  static const String apiBaseUrl = String.fromEnvironment(
    'API_BASE_URL',
    defaultValue: 'http://10.0.2.2:5000/api',
  );
  static const String signalRUrl = String.fromEnvironment(
    'SIGNALR_URL',
    defaultValue: 'http://10.0.2.2:5000',
  );
  static const String accessTokenKey = 'access_token';
  static const String refreshTokenKey = 'refresh_token';
}
