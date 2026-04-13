class UserModel {
  final String id;
  final String username;
  final String email;
  final String? firstName;
  final String? lastName;
  final String role;
  final bool isActive;
  final DateTime createdAt;

  const UserModel({
    required this.id,
    required this.username,
    required this.email,
    this.firstName,
    this.lastName,
    required this.role,
    required this.isActive,
    required this.createdAt,
  });

  factory UserModel.fromJson(Map<String, dynamic> json) => UserModel(
        id: json['id'] as String,
        username: json['username'] as String,
        email: json['email'] as String,
        firstName: json['firstName'] as String?,
        lastName: json['lastName'] as String?,
        role: json['role'] as String,
        isActive: json['isActive'] as bool,
        createdAt: DateTime.parse(json['createdAt'] as String),
      );

  String get displayName =>
      [firstName, lastName].where((s) => s != null && s.isNotEmpty).join(' ').trim().isEmpty
          ? username
          : [firstName, lastName].where((s) => s != null && s.isNotEmpty).join(' ');
}
