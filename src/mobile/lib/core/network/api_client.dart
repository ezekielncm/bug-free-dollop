import 'package:dio/dio.dart';
import '../constants/app_constants.dart';
import '../storage/secure_storage.dart';

class ApiClient {
  late final Dio _dio;
  final SecureStorageService secureStorage;

  ApiClient({required this.secureStorage}) {
    _dio = Dio(BaseOptions(
      baseUrl: AppConstants.apiBaseUrl,
      connectTimeout: const Duration(seconds: 10),
      receiveTimeout: const Duration(seconds: 30),
      headers: {'Content-Type': 'application/json'},
    ));
    _addInterceptors();
  }

  void _addInterceptors() {
    _dio.interceptors.add(InterceptorsWrapper(
      onRequest: (options, handler) async {
        final token = await secureStorage.getAccessToken();
        if (token != null) {
          options.headers['Authorization'] = 'Bearer $token';
        }
        return handler.next(options);
      },
      onError: (error, handler) async {
        if (error.response?.statusCode == 401) {
          try {
            final refreshToken = await secureStorage.getRefreshToken();
            if (refreshToken == null) return handler.reject(error);

            final response = await Dio().post(
              '${AppConstants.apiBaseUrl}/auth/refresh',
              data: {'refreshToken': refreshToken},
            );

            final newToken = response.data['accessToken'] as String;
            final newRefresh = response.data['refreshToken'] as String;
            await secureStorage.saveAccessToken(newToken);
            await secureStorage.saveRefreshToken(newRefresh);

            final retryOptions = error.requestOptions;
            retryOptions.headers['Authorization'] = 'Bearer $newToken';
            final retryResponse = await _dio.fetch(retryOptions);
            return handler.resolve(retryResponse);
          } catch (_) {
            await secureStorage.deleteAll();
            return handler.reject(error);
          }
        }
        return handler.next(error);
      },
    ));
  }

  Future<Response<T>> get<T>(String path, {Map<String, dynamic>? queryParams}) =>
      _dio.get(path, queryParameters: queryParams);

  Future<Response<T>> post<T>(String path, {dynamic data}) =>
      _dio.post(path, data: data);

  Future<Response<T>> put<T>(String path, {dynamic data}) =>
      _dio.put(path, data: data);

  Future<Response<T>> delete<T>(String path) =>
      _dio.delete(path);
}
