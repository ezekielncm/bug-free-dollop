import '../../core/network/api_client.dart';
import '../../core/models/user_model.dart';

class DashboardRepository {
  final ApiClient apiClient;

  DashboardRepository({required this.apiClient});

  Future<UserModel> getCurrentUser() async {
    final response = await apiClient.get<Map<String, dynamic>>('/users/me');
    return UserModel.fromJson(response.data!);
  }
}
