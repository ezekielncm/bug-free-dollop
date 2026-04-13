import 'package:flutter/material.dart';
import 'package:flutter_bloc/flutter_bloc.dart';
import 'package:go_router/go_router.dart';
import 'features/auth/presentation/bloc/auth_bloc.dart';
import 'features/auth/presentation/screens/login_screen.dart';
import 'features/auth/presentation/screens/register_screen.dart';
import 'features/dashboard/presentation/screens/dashboard_screen.dart';

class AppRouter {
  static final router = GoRouter(
    initialLocation: '/login',
    redirect: (context, state) {
      final authState = context.read<AuthBloc>().state;
      final isAuthenticated = authState is AuthenticatedState;
      final isOnAuthPage =
          state.matchedLocation == '/login' || state.matchedLocation == '/register';

      if (!isAuthenticated && !isOnAuthPage) return '/login';
      if (isAuthenticated && isOnAuthPage) return '/dashboard';
      return null;
    },
    refreshListenable: RouterRefreshStream(
      stream: null, // replaced with bloc listener in real usage
    ),
    routes: [
      GoRoute(path: '/login', builder: (_, __) => const LoginScreen()),
      GoRoute(path: '/register', builder: (_, __) => const RegisterScreen()),
      GoRoute(path: '/dashboard', builder: (_, __) => const DashboardScreen()),
    ],
  );
}

// Simple listenable for GoRouter refresh
// TODO: In production, replace `stream` with a broadcast stream from AuthBloc
// e.g.: stream: authBloc.stream.map((_) => null)
class RouterRefreshStream extends ChangeNotifier {
  RouterRefreshStream({dynamic stream});
}
