import '../../core/network/api_client.dart';
import '../../core/storage/secure_storage.dart';
import '../../core/models/user_model.dart';

class AuthRepository {
  final ApiClient apiClient;
  final SecureStorageService storage;

  AuthRepository({required this.apiClient, required this.storage});

  Future<UserModel> login(String email, String password) async {
    final response = await apiClient.post<Map<String, dynamic>>(
      '/auth/login',
      data: {'email': email, 'password': password},
    );
    final data = response.data!;
    await storage.saveAccessToken(data['accessToken'] as String);
    await storage.saveRefreshToken(data['refreshToken'] as String);
    return getMe();
  }

  Future<UserModel> register({
    required String username,
    required String email,
    required String password,
    String? firstName,
    String? lastName,
  }) async {
    final response = await apiClient.post<Map<String, dynamic>>(
      '/auth/register',
      data: {
        'username': username,
        'email': email,
        'password': password,
        if (firstName != null) 'firstName': firstName,
        if (lastName != null) 'lastName': lastName,
      },
    );
    final data = response.data!;
    await storage.saveAccessToken(data['accessToken'] as String);
    await storage.saveRefreshToken(data['refreshToken'] as String);
    return getMe();
  }

  Future<UserModel> getMe() async {
    final response = await apiClient.get<Map<String, dynamic>>('/users/me');
    return UserModel.fromJson(response.data!);
  }

  Future<bool> isLoggedIn() async {
    final token = await storage.getAccessToken();
    return token != null;
  }

  Future<void> logout() async {
    await storage.deleteAll();
  }
}
