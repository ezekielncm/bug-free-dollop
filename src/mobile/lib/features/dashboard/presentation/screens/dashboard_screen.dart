import 'package:flutter/material.dart';
import 'package:flutter_bloc/flutter_bloc.dart';
import '../../../auth/presentation/bloc/auth_bloc.dart';
import '../../../../features/notifications/notification_service.dart';

class DashboardScreen extends StatefulWidget {
  const DashboardScreen({super.key});

  @override
  State<DashboardScreen> createState() => _DashboardScreenState();
}

class _DashboardScreenState extends State<DashboardScreen> {
  final List<String> _notifications = [];

  @override
  void initState() {
    super.initState();
    _connectSignalR();
  }

  void _connectSignalR() {
    final ns = context.read<NotificationService>();
    ns.onNotification = (message) {
      if (mounted) {
        setState(() => _notifications.insert(0, message));
      }
    };
    ns.connect();
  }

  @override
  void dispose() {
    context.read<NotificationService>().disconnect();
    super.dispose();
  }

  @override
  Widget build(BuildContext context) {
    final authState = context.watch<AuthBloc>().state;
    final user = authState is AuthenticatedState ? authState.user : null;

    return Scaffold(
      appBar: AppBar(
        title: const Text('Dashboard'),
        actions: [
          IconButton(
            icon: const Icon(Icons.logout),
            onPressed: () => context.read<AuthBloc>().add(const AuthLogoutEvent()),
          ),
        ],
      ),
      body: RefreshIndicator(
        onRefresh: () async => context.read<AuthBloc>().add(const AuthCheckStatusEvent()),
        child: ListView(
          padding: const EdgeInsets.all(16),
          children: [
            // Welcome card
            Card(
              child: Padding(
                padding: const EdgeInsets.all(16),
                child: Column(
                  crossAxisAlignment: CrossAxisAlignment.start,
                  children: [
                    Text('Welcome, ${user?.displayName ?? 'User'}!',
                        style: const TextStyle(fontSize: 20, fontWeight: FontWeight.bold)),
                    const SizedBox(height: 4),
                    Text(user?.email ?? '', style: const TextStyle(color: Colors.grey)),
                    const SizedBox(height: 8),
                    Chip(
                      label: Text(user?.role ?? ''),
                      backgroundColor: Theme.of(context).colorScheme.primaryContainer,
                    ),
                  ],
                ),
              ),
            ),
            const SizedBox(height: 16),

            // Stats row
            Row(
              children: [
                _StatCard(label: 'Status', value: 'Online', color: Colors.green),
                const SizedBox(width: 8),
                _StatCard(label: 'Real-time', value: 'Connected', color: Colors.purple),
              ],
            ),
            const SizedBox(height: 16),

            // Notifications
            const Text('Live Notifications',
                style: TextStyle(fontSize: 16, fontWeight: FontWeight.bold)),
            const SizedBox(height: 8),
            if (_notifications.isEmpty)
              const Card(
                child: Padding(
                  padding: EdgeInsets.all(16),
                  child: Text('No notifications yet…', style: TextStyle(color: Colors.grey)),
                ),
              )
            else
              ..._notifications.take(20).map((n) => Card(
                    margin: const EdgeInsets.only(bottom: 8),
                    child: ListTile(
                      leading: const Icon(Icons.notifications_outlined),
                      title: Text(n, style: const TextStyle(fontSize: 14)),
                    ),
                  )),
          ],
        ),
      ),
    );
  }
}

class _StatCard extends StatelessWidget {
  final String label;
  final String value;
  final Color color;

  const _StatCard({required this.label, required this.value, required this.color});

  @override
  Widget build(BuildContext context) {
    return Expanded(
      child: Card(
        child: Padding(
          padding: const EdgeInsets.all(16),
          child: Column(
            crossAxisAlignment: CrossAxisAlignment.start,
            children: [
              Text(label, style: const TextStyle(color: Colors.grey, fontSize: 12)),
              const SizedBox(height: 4),
              Text(value, style: TextStyle(color: color, fontWeight: FontWeight.bold, fontSize: 16)),
            ],
          ),
        ),
      ),
    );
  }
}
