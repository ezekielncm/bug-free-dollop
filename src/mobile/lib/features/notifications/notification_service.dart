import 'package:signalr_netcore/signalr_client.dart';
import '../core/constants/app_constants.dart';
import '../core/storage/secure_storage.dart';

class NotificationService {
  final SecureStorageService storage;
  HubConnection? _connection;
  void Function(String message)? onNotification;

  NotificationService({required this.storage});

  Future<void> connect() async {
    final token = await storage.getAccessToken();
    if (token == null) return;

    _connection = HubConnectionBuilder()
        .withUrl(
          '${AppConstants.signalRUrl}/hubs/notifications',
          options: HttpConnectionOptions(
            accessTokenFactory: () async => token,
            logMessageContent: false,
          ),
        )
        .withAutomaticReconnect()
        .build();

    _connection!.on('Notification', (arguments) {
      if (arguments != null && arguments.isNotEmpty) {
        final data = arguments[0] as Map<String, dynamic>?;
        final message = data?['message']?.toString() ?? arguments[0].toString();
        onNotification?.call(message);
      }
    });

    _connection!.on('ReceiveMessage', (arguments) {
      if (arguments != null && arguments.length >= 2) {
        onNotification?.call('${arguments[0]}: ${arguments[1]}');
      }
    });

    try {
      await _connection!.start();
    } catch (e) {
      // Connection failed - silently ignore in background
    }
  }

  Future<void> disconnect() async {
    await _connection?.stop();
    _connection = null;
  }

  Future<void> joinGroup(String groupName) async {
    await _connection?.invoke('JoinGroup', args: [groupName]);
  }

  Future<void> leaveGroup(String groupName) async {
    await _connection?.invoke('LeaveGroup', args: [groupName]);
  }
}
