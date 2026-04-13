import 'package:flutter/material.dart';
import 'package:flutter_bloc/flutter_bloc.dart';
import 'core/constants/app_constants.dart';
import 'core/network/api_client.dart';
import 'core/storage/secure_storage.dart';
import 'features/auth/data/auth_repository.dart';
import 'features/auth/presentation/bloc/auth_bloc.dart';
import 'features/dashboard/data/dashboard_repository.dart';
import 'features/notifications/notification_service.dart';
import 'router.dart';

void main() {
  WidgetsFlutterBinding.ensureInitialized();
  runApp(const MyApp());
}

class MyApp extends StatelessWidget {
  const MyApp({super.key});

  @override
  Widget build(BuildContext context) {
    final secureStorage = SecureStorageService();
    final apiClient = ApiClient(secureStorage: secureStorage);
    final authRepository = AuthRepository(apiClient: apiClient, storage: secureStorage);
    final dashboardRepository = DashboardRepository(apiClient: apiClient);
    final notificationService = NotificationService(storage: secureStorage);

    return MultiRepositoryProvider(
      providers: [
        RepositoryProvider.value(value: authRepository),
        RepositoryProvider.value(value: dashboardRepository),
        RepositoryProvider.value(value: notificationService),
      ],
      child: BlocProvider(
        create: (_) => AuthBloc(authRepository: authRepository)..add(const AuthCheckStatusEvent()),
        child: MaterialApp.router(
          title: 'MyApp',
          debugShowCheckedModeBanner: false,
          theme: ThemeData(
            colorScheme: ColorScheme.fromSeed(seedColor: const Color(0xFF0284C7)),
            useMaterial3: true,
          ),
          routerConfig: AppRouter.router,
        ),
      ),
    );
  }
}
