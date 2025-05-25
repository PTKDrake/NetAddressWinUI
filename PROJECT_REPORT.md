# BÁO CÁO DỰ ÁN: HỆ THỐNG QUẢN LÝ THIẾT BỊ TỪ XA
## Remote Device Management System với Admin Dashboard

**Môn học:** Lập trình mạng  
**Sinh viên:** [Tên sinh viên]  
**Mã số sinh viên:** [MSSV]  
**Ngày báo cáo:** [Ngày/Tháng/Năm]

---

## 1. TỔNG QUAN DỰ ÁN

### 1.1 Mô tả dự án
Hệ thống quản lý thiết bị từ xa (Remote Device Management System) là một ứng dụng web full-stack cho phép quản lý và giám sát các thiết bị máy tính từ xa thông qua kết nối mạng. Hệ thống được xây dựng với NuxtJS 4 và cung cấp hai mức quyền truy cập khác nhau:

**Tính năng chính:**
- **Real-time monitoring**: Giám sát thiết bị theo thời gian thực với Socket.IO
- **Hardware information tracking**: Thu thập thông tin phần cứng chi tiết (CPU, RAM, Storage, GPU, Network, OS, Motherboard)
- **Remote control**: Điều khiển thiết bị từ xa (shutdown commands)
- **Multi-user authentication**: Xác thực và phân quyền người dùng với Better Auth
- **Admin dashboard**: Quản trị viên có thể xem và quản lý thiết bị của tất cả người dùng
- **User filtering**: Admin có thể lọc thiết bị theo từng người dùng cụ thể
- **Device registration**: Đăng ký và quản lý thiết bị tự động qua WebSocket

### 1.2 Kiến trúc hệ thống
```
[Client Devices] ←→ [WebSocket Server] ←→ [NuxtJS 4 Application] ←→ [PostgreSQL + Drizzle ORM]
                          ↕                        ↕
                   [Socket.IO Server] ←→ [Better Auth Session]
                          ↕                        ↕
                   [Web Browser Client]    [Admin Dashboard]
```

### 1.3 Phân quyền người dùng
- **Regular User**: Chỉ có thể xem và quản lý thiết bị của chính mình
- **Admin User**: 
  - Xem tất cả thiết bị của toàn bộ người dùng
  - Lọc thiết bị theo người dùng cụ thể
  - Shutdown bất kỳ thiết bị nào
  - Truy cập admin dashboard tại `/admin/devices`

### 1.4 Tech Stack
- **Frontend**: NuxtJS 4, @nuxt/ui, Tailwind CSS, TypeScript
- **Backend**: Better Auth, Drizzle ORM, Socket.IO, WebSocket
- **Database**: PostgreSQL với JSONB support
- **Package Manager**: pnpm
- **Authentication**: Google OAuth, Email/Password với Better Auth

---

## 2. CÔNG NGHỆ VÀ GIAO THỨC MẠNG SỬ DỤNG

### 2.1 Giao thức mạng chính

#### 2.1.1 HTTP/HTTPS Protocol
- **Mục đích**: Giao tiếp RESTful API giữa NuxtJS client và server
- **Port**: 3000 (Development), 80/443 (Production)
- **Framework**: NuxtJS 4 với Server Routes
- **Methods sử dụng**:
  - `GET`: Lấy danh sách thiết bị (với phân quyền admin/user), thông tin người dùng
  - `POST`: Đăng ký thiết bị, xác thực người dùng, gửi lệnh shutdown, debug events
  - `PUT`: Cập nhật thông tin thiết bị
  - `DELETE`: Xóa thiết bị

#### 2.1.2 WebSocket Protocol (RFC 6455)
- **Mục đích**: Kết nối persistent full-duplex giữa client devices và NuxtJS server
- **Port**: 3000/websocket (Development), 80/443 (Production)
- **Implementation**: Native WebSocket với ws library
- **Đặc điểm**:
  - Persistent connection: Duy trì kết nối liên tục cho device registration
  - Low latency: Độ trễ thấp cho real-time hardware monitoring
  - Bidirectional: Giao tiếp hai chiều (device updates, shutdown commands)
  - Message validation: Zod schema validation cho tất cả messages

#### 2.1.3 Socket.IO Protocol
- **Mục đích**: Real-time communication giữa NuxtJS web client và server
- **Port**: 3000 (embedded trong HTTP server)
- **Implementation**: Socket.IO v4 với singleton pattern
- **Transport methods**:
  - WebSocket (primary)
  - HTTP long-polling (fallback)
- **Features**:
  - Auto-reconnection với exponential backoff
  - Component-based event management
  - Real-time device list updates
  - Admin/User role-based broadcasting
  - Event deduplication để tránh spam notifications

### 2.2 Giao thức tầng thấp

#### 2.2.1 TCP (Transmission Control Protocol)
- **Vai trò**: Transport layer protocol cho WebSocket và HTTP
- **Đặc điểm**:
  - Reliable delivery: Đảm bảo gửi tin cậy
  - Connection-oriented: Thiết lập kết nối trước khi truyền
  - Flow control: Kiểm soát luồng dữ liệu
  - Error detection và correction

#### 2.2.2 IP (Internet Protocol)
- **Version**: IPv4 và IPv6
- **Vai trò**: Network layer routing
- **Addressing**: Định danh thiết bị qua IP address

---

## 3. CẤU TRÚC DỮ LIỆU VÀ ĐÓNG GÓI

### 3.1 WebSocket Message Format

#### 3.1.1 Device Registration Message
```json
{
  "messageType": "register",
  "userId": "user123",
  "macAddress": "00:11:22:33:44:55",
  "ipAddress": "192.168.1.100",
  "machineName": "My Computer",
  "hardware": {
    "cpu": {
      "model": "Intel Core i7-12700K",
      "cores": 12,
      "speed": 3.6,
      "usage": 25.5
    },
    "memory": {
      "total": 32,
      "used": 16.5,
      "available": 15.5,
      "usage": 51.6
    },
    "storage": {
      "total": 1000,
      "used": 450,
      "available": 550,
      "usage": 45.0
    },
    "gpu": {
      "model": "NVIDIA RTX 4070",
      "memory": 12,
      "usage": 15.2
    },
    "network": {
      "interfaces": [
        {
          "name": "Ethernet",
          "type": "ethernet",
          "speed": 1000
        }
      ]
    },
    "os": {
      "name": "Windows",
      "version": "11",
      "architecture": "x64",
      "uptime": 86400
    },
    "motherboard": {
      "manufacturer": "ASUS",
      "model": "ROG STRIX Z690-E"
    }
  }
}
```

#### 3.1.2 Device Update Message
```json
{
  "messageType": "update",
  "macAddress": "00:11:22:33:44:55",
  "ipAddress": "192.168.1.101",
  "machineName": "Updated Computer Name",
  "hardware": {
    // Hardware information với các metrics được cập nhật
  }
}
```

#### 3.1.3 Control Commands
```json
{
  "messageType": "shutdown",
  "macAddress": "00:11:22:33:44:55"
}
```

### 3.2 Socket.IO Events và Component Management

#### 3.2.1 Client to Server Events
- `get-devices`: Lấy danh sách thiết bị (với phân quyền admin/user)
- `shutdown-request`: Yêu cầu tắt thiết bị (với phân quyền admin/user)
- `disconnect`: Ngắt kết nối

#### 3.2.2 Server to Client Events
- `devices-response`: Response danh sách thiết bị với unique requestId
- `device-update`: Cập nhật thông tin thiết bị real-time
- `device-disconnect`: Thiết bị ngắt kết nối
- `device-shutdown`: Thiết bị được tắt
- `shutdown-response`: Response kết quả shutdown command
- `reconnect`: Tự động reconnect và refresh data

#### 3.2.3 Socket Management Pattern
```typescript
// Singleton Socket Manager
class SocketManager {
  private components: Map<string, ComponentCallbacks>
  private socket: Socket | null
  
  // Component registration với unique IDs
  registerComponent(id: string, callbacks: ComponentCallbacks)
  unregisterComponent(id: string)
  
  // Centralized event broadcasting
  private setupConsolidatedListeners()
  private broadcastToAllComponents(event: string, data: any)
}

// Component ID format: device-list-{path}-{role}
// Example: device-list-/devices-user, device-list-/admin/devices-admin
```

### 3.3 HTTP API Endpoints (NuxtJS Server Routes)

#### 3.3.1 Device Management
```
GET    /api/devices              - Lấy danh sách thiết bị (admin: tất cả, user: chỉ của mình)
POST   /api/devices/shutdown     - Gửi lệnh tắt thiết bị (với phân quyền)
PUT    /api/devices/:id          - Cập nhật thiết bị
DELETE /api/devices/:id          - Xóa thiết bị
```

#### 3.3.2 Admin Management
```
GET    /api/admin/users          - Lấy danh sách users (admin only)
GET    /api/admin/devices        - Deprecated - sử dụng /api/devices thay thế
```

#### 3.3.3 Authentication (Better Auth Integration)
```
POST   /api/auth/sign-in         - Đăng nhập (email/password, Google OAuth)
POST   /api/auth/sign-up         - Đăng ký
POST   /api/auth/sign-out        - Đăng xuất
POST   /api/auth/verify-email    - Xác thực email
GET    /api/auth/session         - Lấy session hiện tại
```

#### 3.3.4 Debug Tools (Development Only)
```
POST   /api/debug/trigger-event  - Trigger manual events cho testing
GET    /debug/socket            - Socket debug dashboard
```

---

## 4. QUÁ TRÌNH ĐÓNG GÓI VÀ TRUYỀN DỮ LIỆU

### 4.1 WebSocket Data Flow

#### 4.1.1 Connection Establishment
```
1. HTTP Handshake Request:
   GET /websocket HTTP/1.1
   Host: localhost:3000
   Upgrade: websocket
   Connection: Upgrade
   Sec-WebSocket-Key: [base64-encoded-key]
   Sec-WebSocket-Version: 13

2. HTTP Handshake Response:
   HTTP/1.1 101 Switching Protocols
   Upgrade: websocket
   Connection: Upgrade
   Sec-WebSocket-Accept: [calculated-accept-key]

3. WebSocket Connection Established
```

#### 4.1.2 Data Frame Structure
```
WebSocket Frame Format (RFC 6455):
 0                   1                   2                   3
 0 1 2 3 4 5 6 7 8 9 0 1 2 3 4 5 6 7 8 9 0 1 2 3 4 5 6 7 8 9 0 1
+-+-+-+-+-------+-+-------------+-------------------------------+
|F|R|R|R| opcode|M| Payload len |    Extended payload length    |
|I|S|S|S|  (4)  |A|     (7)     |             (16/64)           |
|N|V|V|V|       |S|             |   (if payload len==126/127)   |
| |1|2|3|       |K|             |                               |
+-+-+-+-+-------+-+-------------+ - - - - - - - - - - - - - - - +
|     Extended payload length continued, if payload len == 127  |
+ - - - - - - - - - - - - - - - +-------------------------------+
|                               |Masking-key, if MASK set to 1  |
+-------------------------------+-------------------------------+
| Masking-key (continued)       |          Payload Data         |
+-------------------------------- - - - - - - - - - - - - - - - +
:                     Payload Data continued ...                :
+ - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - +
|                     Payload Data continued ...                |
+---------------------------------------------------------------+
```

### 4.2 Message Validation và Processing

#### 4.2.1 Zod Schema Validation
```typescript
const RegisterSchema = z.object({
  messageType: z.literal("register"),
  userId: z.string().optional(),
  macAddress: z.string().min(1, "MAC address is required"),
  ipAddress: z.string().min(1, "IP address is required"),
  machineName: z.string().min(1, "Machine name is required"),
  hardware: HardwareSchema
});
```

#### 4.2.2 Message Processing Flow
```
Client Device → WebSocket Frame → JSON Parse → Schema Validation → 
Message Processor → Database Update → Broadcast Event → 
Socket.IO → Web Client Update
```

### 4.3 Database Storage (PostgreSQL + Drizzle ORM)

#### 4.3.1 Device Schema (Drizzle ORM)
```typescript
// /db/deviceSchema.ts
export const devices = pgTable('devices', {
  macAddress: varchar('mac_address', { length: 17 }).primaryKey(),
  userId: text('user_id').notNull().references(() => user.id),
  name: varchar('name', { length: 100 }).notNull(),
  ipAddress: varchar('ip_address', { length: 45 }).notNull(),
  isConnected: boolean('is_connected').default(false).notNull(),
  hardware: jsonb('hardware').$type<HardwareInfo>(),
  lastSeen: timestamp('last_seen').defaultNow(),
  createdAt: timestamp('created_at').defaultNow().notNull(),
  updatedAt: timestamp('updated_at').defaultNow().notNull()
});

// Admin queries with user information
db.select({
  ...devices,
  userName: user.name,
  userEmail: user.email
}).from(devices).leftJoin(user, eq(devices.userId, user.id));
```

#### 4.3.2 User Schema (Better Auth Integration)
```typescript
// Better Auth tự động tạo user table
export const user = pgTable('user', {
  id: text('id').primaryKey(),
  name: text('name').notNull(),
  email: text('email').notNull().unique(),
  emailVerified: boolean('emailVerified').notNull(),
  image: text('image'),
  role: text('role').default('user'), // 'user' | 'admin'
  createdAt: timestamp('createdAt').notNull(),
  updatedAt: timestamp('updatedAt').notNull()
});
```

#### 4.3.2 Hardware Information Structure
```json
{
  "cpu": { "model": "string", "cores": "number", "speed": "number", "usage": "number" },
  "memory": { "total": "number", "used": "number", "available": "number", "usage": "number" },
  "storage": { "total": "number", "used": "number", "available": "number", "usage": "number" },
  "gpu": { "model": "string", "memory": "number", "usage": "number" },
  "network": { "interfaces": "array" },
  "os": { "name": "string", "version": "string", "architecture": "string", "uptime": "number" },
  "motherboard": { "manufacturer": "string", "model": "string" }
}
```

---

## 5. BẢO MẬT VÀ XÁC THỰC

### 5.1 Authentication Flow (Better Auth)
```
1. User Registration/Login → Better Auth với Google OAuth hoặc Email/Password
2. Session-based Authentication (không dùng JWT)
3. Server-side Session Management với Database
4. Role-based Access Control (admin/user)
5. Device Registration với User ID từ session
6. MAC Address validation và uniqueness check
```

### 5.2 Authorization System
```typescript
// Admin role check
const userRole = session.user.role;
const isAdmin = userRole === 'admin' || 
  (Array.isArray(userRole) && userRole.includes('admin'));

// Device access control
if (isAdmin) {
  // Admin can see all devices with user information
  query = db.select({...devices, userName: user.name, userEmail: user.email})
    .from(devices).leftJoin(user, eq(devices.userId, user.id));
} else {
  // Regular users only see their own devices
  query = db.select().from(devices).where(eq(devices.userId, userId));
}
```

### 5.3 Security Measures
- **HTTPS**: Mã hóa dữ liệu truyền tải cho production
- **Session-based Auth**: Secure session management với Better Auth
- **Input Validation**: Comprehensive Zod schema validation cho tất cả inputs
- **CORS**: Cross-Origin Resource Sharing control
- **Role-based Access**: Admin/User permission system
- **SQL Injection Prevention**: Drizzle ORM prepared statements
- **XSS Protection**: Input sanitization và CSP headers

---

## 6. HIỆU NĂNG VÀ TỐI ƯU HÓA

### 6.1 Socket.IO Optimizations
- **Singleton Pattern**: Một Socket.IO instance duy nhất cho toàn bộ app
- **Component-based Management**: Component registration với unique IDs
- **Event Deduplication**: Tránh duplicate events và spam notifications
- **Automatic Reconnection**: Exponential backoff strategy
- **Fallback Strategy**: HTTP API fallback khi Socket.IO không khả dụng

### 6.2 WebSocket Optimizations  
- **Connection Pooling**: Efficient connection management cho device clients
- **Message Validation**: Zod schema validation để tránh invalid data
- **Heartbeat Mechanism**: Keep-alive để maintain persistent connections
- **Error Handling**: Comprehensive error handling và retry logic

### 6.3 Database Optimizations (PostgreSQL + Drizzle)
- **Indexing**: Indexes trên MAC address (primary key) và user ID (foreign key)
- **JSONB Fields**: Efficient storage và querying cho hardware information
- **Connection Pooling**: Drizzle ORM connection management
- **Query Optimization**: Optimized joins cho admin queries với user information
- **Prepared Statements**: SQL injection prevention và performance

### 6.4 Frontend Optimizations (NuxtJS 4)
- **SSR/SSG**: Server-Side Rendering với Nuxt.js 4
- **Code Splitting**: Automatic code splitting và lazy loading
- **Component Caching**: Vue component caching
- **Client-side Hydration**: Optimal hydration strategy
- **Asset Optimization**: Built-in Vite optimizations
- **Real-time Updates**: Efficient reactive updates với Vue 3 Composition API

### 6.5 Development Optimizations
- **HMR**: Hot Module Replacement với Vite
- **TypeScript**: Type safety và better DX
- **Debug Tools**: Browser console debug access và debug dashboard
- **Monitoring**: Real-time connection monitoring và logging

---

## 7. TESTING VÀ DEBUGGING

### 7.1 Debug Tools Implementation
```typescript
// Browser Console Debug Access
window.__deviceListDebug = {
  devices: devices.value,
  uniqueUsers: uniqueUsers.value,
  filteredDevices: filteredDevices.value,
  isAdmin: isAdmin.value,
  currentUser: currentUser.value,
  selectedUserFilter: selectedUserFilter.value,
  componentId: componentId.value
};

// Real-time monitoring
const monitor = setInterval(() => {
  console.log('📊 Live Data:', {
    deviceCount: window.__deviceListDebug.devices.length,
    onlineDevices: window.__deviceListDebug.devices.filter(d => d.isConnected).length,
    uniqueUsers: window.__deviceListDebug.uniqueUsers.length
  });
}, 5000);
```

### 7.2 Socket.IO Testing
```typescript
// Manual event triggering for testing
await $fetch('/api/debug/trigger-event', {
  method: 'POST',
  body: {
    type: 'device-update',
    macAddress: 'test-mac',
    deviceName: 'Test Device'
  }
});

// Component-based testing
const testComponent = 'device-list-/devices-admin';
setupDeviceListeners(testComponent, {
  onDeviceUpdate: (device) => console.log('Update:', device),
  onDeviceDisconnect: (mac) => console.log('Disconnect:', mac)
});
```

### 7.3 WebSocket Testing 
```javascript
// Device client WebSocket test
const ws = new WebSocket('ws://localhost:3000/websocket');

ws.onopen = () => {
  ws.send(JSON.stringify({
    messageType: "register",
    userId: "test-user-id",
    macAddress: "00:11:22:33:44:55",
    ipAddress: "192.168.1.100",
    machineName: "Test Machine",
    hardware: { /* complete hardware info */ }
  }));
};
```

### 7.4 Error Handling Strategy
- **Socket.IO Errors**: Automatic reconnection với exponential backoff
- **WebSocket Errors**: Connection retry mechanisms
- **Validation Errors**: Comprehensive Zod schema error reporting
- **Network Timeouts**: Graceful timeout handling với fallbacks
- **Database Errors**: Transaction rollback và proper error logging
- **Auth Errors**: Session validation và redirect handling

### 7.5 Debugging Features
- **Debug Dashboard**: `/debug/socket` page với real-time monitoring
- **Console Access**: `window.__deviceListDebug` cho runtime inspection
- **Debug Widget**: Floating debug widget cho admin users
- **Event Logging**: Comprehensive logging throughout the system
- **Connection Monitoring**: Real-time Socket.IO connection status

---

## 8. DEPLOYMENT VÀ PRODUCTION

### 8.1 Production Architecture (NuxtJS 4)
```
[Load Balancer/CDN] → [Nginx Reverse Proxy] → [NuxtJS 4 Server]
                                                      ↓
[Device WebSocket] ← [Socket.IO Server] ← [Better Auth Session]
                                                      ↓
                                              [PostgreSQL + Drizzle ORM]
```

### 8.2 Development vs Production
```typescript
// Development (localhost:3000)
export default defineNuxtConfig({
  devtools: { enabled: true },
  ssr: true,
  nitro: {
    experimental: {
      wasm: true
    }
  }
});

// Production
export default defineNuxtConfig({
  ssr: true,
  nitro: {
    preset: 'node-server', // or 'vercel', 'cloudflare', etc.
  },
  runtimeConfig: {
    authSecret: process.env.AUTH_SECRET,
    googleClientId: process.env.GOOGLE_CLIENT_ID,
    googleClientSecret: process.env.GOOGLE_CLIENT_SECRET,
    databaseUrl: process.env.DATABASE_URL
  }
});
```

### 8.3 Scalability Considerations
- **Horizontal Scaling**: Multiple NuxtJS instances với shared PostgreSQL
- **Database Replication**: PostgreSQL master-slave configuration
- **Socket.IO Scaling**: Redis adapter cho multiple instances
- **CDN**: Static asset delivery via Vercel/Cloudflare
- **Session Storage**: Database-backed sessions cho consistency

---

## 9. KẾT LUẬN

### 9.1 Kết quả đạt được
- **✅ Full-stack Application**: Xây dựng thành công hệ thống quản lý thiết bị từ xa với NuxtJS 4
- **✅ Real-time Communication**: Triển khai dual-protocol system (WebSocket cho devices, Socket.IO cho web clients)
- **✅ Admin Dashboard**: Implement admin system với cross-user device visibility và user filtering
- **✅ Role-based Access Control**: Phân quyền admin/user với Better Auth integration
- **✅ Hardware Monitoring**: Thu thập và hiển thị thông tin phần cứng chi tiết real-time
- **✅ Socket Management**: Implement singleton pattern để tránh duplicate events
- **✅ Debug Tools**: Comprehensive debugging system với browser console access

### 9.2 Kiến thức thu được
- **Modern Web Development**: NuxtJS 4, Vue 3 Composition API, TypeScript
- **Network Protocols**: HTTP/HTTPS, WebSocket (RFC 6455), Socket.IO Protocol
- **Real-time Communication**: Dual-protocol architecture, event management, connection handling
- **Database Design**: PostgreSQL với Drizzle ORM, JSONB data storage, query optimization
- **Authentication**: Better Auth implementation, session management, role-based authorization
- **Security**: Input validation với Zod, SQL injection prevention, XSS protection
- **Performance Optimization**: Component-based Socket management, efficient database queries
- **Development Tools**: Debug dashboard, browser console integration, monitoring systems

### 9.3 Challenges & Solutions
- **Socket Event Conflicts**: Solved với singleton SocketManager pattern
- **HMR Issues**: Handled với proper component cleanup và connection management
- **Admin Permissions**: Implemented role-based queries với database joins
- **Real-time Updates**: Optimized với event deduplication và component-based broadcasting

### 9.4 Hướng phát triển
- **Production Deployment**: SSL/TLS certificates, environment configuration
- **Monitoring System**: Comprehensive logging và performance monitoring
- **Mobile Support**: React Native hoặc mobile-responsive design
- **Enhanced Security**: Rate limiting, advanced authentication methods
- **Scalability**: Redis adapter cho Socket.IO clustering
- **Advanced Features**: Device grouping, scheduled tasks, remote file management
- **Analytics**: Usage statistics, performance metrics dashboard

---

## 10. TÀI LIỆU THAM KHẢO

### 10.1 Network Protocols & Standards
1. **RFC 6455** - The WebSocket Protocol
2. **HTTP/1.1 Specification** - RFC 7230-7237
3. **TCP/IP Illustrated** - Richard Stevens
4. **Socket.IO Protocol** - https://socket.io/docs/v4/

### 10.2 Framework & Library Documentation
5. **NuxtJS 4 Documentation** - https://nuxt.com/
6. **Vue 3 Composition API** - https://vuejs.org/guide/
7. **Better Auth Documentation** - https://better-auth.com/
8. **Drizzle ORM Documentation** - https://orm.drizzle.team/
9. **@nuxt/ui Components** - https://ui.nuxt.com/
10. **Tailwind CSS** - https://tailwindcss.com/

### 10.3 Database & Development Tools
11. **PostgreSQL Documentation** - https://postgresql.org/docs/
12. **TypeScript Handbook** - https://typescriptlang.org/docs/
13. **Zod Schema Validation** - https://zod.dev/
14. **pnpm Package Manager** - https://pnpm.io/

### 10.4 Deployment & Production
15. **Vercel Deployment** - https://vercel.com/docs
16. **Docker Documentation** - https://docs.docker.com/
17. **Nginx Configuration** - https://nginx.org/en/docs/

---

## 11. PHỤ LỤC

### 11.1 Project Structure
```
net-address-web/
├── app/
│   ├── components/          # Vue components
│   │   ├── DeviceList.client.vue
│   │   ├── socket.ts        # Socket management
│   │   └── SocketDebugWidget.vue
│   ├── pages/               # NuxtJS pages
│   │   ├── devices.vue
│   │   ├── admin/
│   │   │   └── devices.vue
│   │   └── debug/
│   │       └── socket.vue
│   └── assets/
│       └── styles.css       # Global styles
├── server/
│   ├── api/                 # API routes
│   │   ├── devices.get.ts
│   │   ├── devices/
│   │   │   └── shutdown.post.ts
│   │   ├── admin/
│   │   │   └── users.get.ts
│   │   └── debug/
│   │       └── trigger-event.post.ts
│   └── utils/
│       ├── drizzle.ts       # Database connection
│       ├── websocket.ts     # WebSocket server
│       └── socket-io.ts     # Socket.IO server
├── db/
│   ├── deviceSchema.ts      # Device schema
│   └── authSchema.ts        # Auth schema
├── lib/
│   ├── auth.ts              # Better Auth config
│   └── auth-client.ts       # Client-side auth
├── nuxt.config.ts           # NuxtJS configuration
├── package.json             # Dependencies (pnpm)
└── drizzle.config.ts        # Database config
```

### 11.2 Database Schema
```sql
-- Users table (Better Auth)
CREATE TABLE user (
    id TEXT PRIMARY KEY,
    name TEXT NOT NULL,
    email TEXT UNIQUE NOT NULL,
    email_verified BOOLEAN NOT NULL,
    image TEXT,
    role TEXT DEFAULT 'user',
    created_at TIMESTAMP NOT NULL,
    updated_at TIMESTAMP NOT NULL
);

-- Devices table
CREATE TABLE devices (
    mac_address VARCHAR(17) PRIMARY KEY,
    user_id TEXT NOT NULL REFERENCES user(id),
    name VARCHAR(100) NOT NULL,
    ip_address VARCHAR(45) NOT NULL,
    is_connected BOOLEAN DEFAULT FALSE,
    hardware JSONB,
    last_seen TIMESTAMP DEFAULT NOW(),
    created_at TIMESTAMP DEFAULT NOW(),
    updated_at TIMESTAMP DEFAULT NOW()
);

-- Indexes
CREATE INDEX idx_devices_user_id ON devices(user_id);
CREATE INDEX idx_devices_is_connected ON devices(is_connected);
```

---

**Ngày hoàn thành:** 26/05/2025  
**Project Repository:** [GitHub Link]  
**Live Demo:** [Production URL]  
**WinUI Client:** Windows Device Management Client

---

## 12. WINUI CLIENT APPLICATION - DEVICE AGENT

### 12.1 Tổng quan WinUI Client Application

WinUI Client Application đóng vai trò là **Device Agent** - một ứng dụng Windows chạy trên các thiết bị client để kết nối với Remote Device Management Server. Ứng dụng được xây dựng với **WinUI 3** và **Windows App SDK**, sử dụng kiến trúc **MVVM** với **CommunityToolkit.Mvvm**.

#### 12.1.1 Vai trò và chức năng chính
- **Device Registration**: Tự động đăng ký thiết bị với server thông qua WebSocket
- **Hardware Monitoring**: Thu thập và gửi thông tin phần cứng real-time
- **Remote Control**: Nhận và thực thi các lệnh điều khiển từ xa (shutdown commands)
- **System Tray Integration**: Chạy nền với taskbar icon integration
- **Authentication**: Xác thực người dùng qua Better Auth server
- **Auto-reconnection**: Tự động kết nối lại khi mất kết nối

#### 12.1.2 Technology Stack WinUI Client
```
┌─────────────────────────────────────────────────────────────┐
│                    WinUI 3 Client (.NET 9)                  │
├─────────────────────────────────────────────────────────────┤
│ UI Layer: DevWinUI Components + Fluent Design               │
│ MVVM: CommunityToolkit.Mvvm + Data Binding                  │
│ Services: WebSocket + Hardware Info + Auth                  │
│ Data: JsonSettings + Hardware.Info + System.Management     │
│ Platform: Windows App SDK 1.7 + WinRT                      │
└─────────────────────────────────────────────────────────────┘
```

### 12.2 Kiến trúc WinUI Client Application

#### 12.2.1 Project Structure
```
NetAddressWinUI/
├── App.xaml.cs                    # Application entry point & DI configuration
├── MainWindow.xaml                # Main application window
├── GlobalUsings.cs                # Global namespace imports
├── NetAddressWinUI.csproj         # Project configuration & dependencies
├── Views/                         # XAML User Interface
│   ├── HomePage.xaml              # Main dashboard với hardware info
│   ├── LoginPage.xaml             # Authentication UI
│   ├── SettingsPage.xaml          # Settings configuration
│   └── Settings/                  
│       ├── GeneralSettingPage.xaml # General settings (tray, shutdown)
│       ├── AccountSettingPage.xaml # Account management
│       └── ThemeSettingPage.xaml   # Theme customization
├── ViewModels/                    # MVVM ViewModels với CommunityToolkit.Mvvm
│   ├── HomeViewModel.cs           # Hardware monitoring & WebSocket logic
│   ├── LoginViewModel.cs          # Authentication logic
│   ├── MainViewModel.cs           # Main navigation logic
│   └── Settings/                  
│       ├── GeneralSettingViewModel.cs
│       ├── AccountSettingViewModel.cs
│       └── AboutUsSettingViewModel.cs
├── Services/                      # Business Logic Services
│   ├── WebSocketService.cs        # WebSocket client communication
│   ├── HardwareInfoService.cs     # Hardware information collection
│   ├── HttpService.cs             # HTTP API client
│   └── AuthService.cs             # Authentication service
├── Models/                        # Data Models & DTOs
│   ├── HardwareInfo.cs            # Hardware data structures
│   ├── AuthModels.cs              # Authentication models
│   └── WebSocketModels.cs         # WebSocket message models
├── Common/                        # Shared utilities
│   ├── AppConfig.cs               # Application configuration
│   ├── ThemeConfig.cs             # Theme settings
│   └── Constants.cs               # Application constants
├── Assets/                        # Application resources
├── Themes/                        # Custom theme resources
└── Properties/                    # Application manifest & properties
```

#### 12.2.2 MVVM Architecture Pattern
```csharp
// CommunityToolkit.Mvvm implementation
public partial class HomeViewModel : ObservableObject
{
    [ObservableProperty]
    private HardwareInfo hardwareInfo = new();
    
    [ObservableProperty] 
    private bool connected = false;
    
    [RelayCommand]
    public async Task Connect()
    {
        // WebSocket connection logic
    }
    
    [RelayCommand]
    public async Task Disconnect()
    {
        // Disconnect logic
    }
}
```

### 12.3 Network Communication - Client Side

#### 12.3.1 WebSocket Client Implementation
```csharp
// Native Windows Runtime WebSocket
public class WebSocketService : IWebSocketService
{
    private MessageWebSocket _socket;
    private DataWriter _writer;

    public async Task ConnectAsync(Uri uri, CancellationToken cancellationToken = default)
    {
        _socket = new MessageWebSocket();
        _socket.Control.MessageType = SocketMessageType.Utf8;
        _socket.MessageReceived += OnMessageReceived;
        
        await _socket.ConnectAsync(uri).AsTask(cancellationToken);
        _writer = new DataWriter(_socket.OutputStream);
    }

    public async Task SendMessageAsync<T>(T message, CancellationToken cancellationToken = default)
    {
        string json = JsonSerializer.Serialize(message);
        _writer.WriteString(json);
        await _writer.StoreAsync().AsTask(cancellationToken);
        await _writer.FlushAsync().AsTask(cancellationToken);
    }
}
```

#### 12.3.2 Device Registration Protocol
```csharp
// Automatic device registration on connection
var registerMessage = new RegisterMessage
{
    messageType = "register",
    userId = Settings.UserId,
    macAddress = GetMacAddress(),
    ipAddress = GetIpAddress(), 
    machineName = Environment.MachineName,
    hardware = await _hardwareInfoService.GetHardwareInfoAsync()
};

await _webSocketService.SendMessageAsync(registerMessage);
```

#### 12.3.3 Client Configuration (AppConfig.cs)
```csharp
[GenerateAutoSaveOnChange]
public partial class AppConfig : NotifiyingJsonSettings, IVersionable
{
    private string webUrl { get; set; } = "http://localhost:3000";
    private string apiUrl { get; set; } = "http://localhost:3000/api";
    private string webSocketUrl { get; set; } = "ws://localhost:3000/api/websocket";
    private string? token { get; set; } = null;
    private string? userId { get; set; } = null;
    private bool useTrayIcon { get; set; } = true;
    private bool realShutDown { get; set; } = false;
}
```

### 12.4 Hardware Information Collection

#### 12.4.1 Hardware Monitoring Architecture
```csharp
public class HardwareInfoService : IHardwareInfoService
{
    // Multi-threaded hardware data collection
    public async Task<HardwareInfo> GetHardwareInfoAsync()
    {
        var hardwareInfo = new HardwareInfo();
        
        var tasks = new Task[]
        {
            Task.Run(() => hardwareInfo.cpu = GetCpuInfo()),
            Task.Run(() => hardwareInfo.memory = GetMemoryInfo()),
            Task.Run(() => hardwareInfo.storage = GetStorageInfo()),
            Task.Run(() => hardwareInfo.gpu = GetGpuInfo()),
            Task.Run(() => hardwareInfo.network = GetNetworkInfo()),
            Task.Run(() => hardwareInfo.os = GetOsInfo()),
            Task.Run(() => hardwareInfo.motherboard = GetMotherboardInfo())
        };
        
        await Task.WhenAll(tasks).ConfigureAwait(false);
        return hardwareInfo;
    }
}
```

#### 12.4.2 Hardware Data Sources & APIs
- **CPU Information**: 
  - `System.Management` (WMI) - `Win32_Processor`
  - `PerformanceCounter` - Real-time CPU usage
  - Fallback: `Environment.ProcessorCount`

- **Memory Information**:
  - WMI `Win32_ComputerSystem` - Total physical memory
  - `PerformanceCounter` - Available memory counters
  - `GC.GetTotalMemory()` - Managed memory

- **Storage Information**:
  - `DriveInfo.GetDrives()` - Drive enumeration
  - WMI `Win32_LogicalDisk` - Disk space information

- **GPU Information**:
  - WMI `Win32_VideoController` - GPU model và memory
  - Performance counters - GPU usage (when available)

- **Network Information**:
  - `NetworkInterface.GetAllNetworkInterfaces()` - Network adapters
  - `System.Net.NetworkInformation` - Network statistics

- **OS Information**:
  - `Environment.OSVersion` - Operating system version
  - `Environment.MachineName` - Machine name
  - `Environment.GetTickCount64()` - System uptime

- **Motherboard Information**:
  - WMI `Win32_BaseBoard` - Motherboard manufacturer và model

#### 12.4.3 Performance Optimizations
```csharp
// Static information caching
private static CpuInfo? _cachedCpuInfo;
private static DateTime _lastStaticInfoUpdate = DateTime.MinValue;
private static readonly TimeSpan _staticInfoCacheTime = TimeSpan.FromMinutes(5);

// Performance counter management
private static PerformanceCounter? _cpuCounter;
private static readonly object _cpuCounterLock = new object();

// Fallback methods for error handling
private CpuInfo GetCpuInfoFallback()
{
    return new CpuInfo
    {
        model = "Unknown CPU",
        cores = Environment.ProcessorCount,
        speed = 0,
        usage = 0
    };
}
```

### 12.5 Real-time Data Updates

#### 12.5.1 Hardware Update Timer
```csharp
// Initialize regular hardware updates
private void InitializeHardwareUpdates()
{
    // Load initial data immediately
    Task.Run(async () =>
    {
        var initialHardwareInfo = await _hardwareInfoService.GetHardwareInfoAsync();
        _dispatcherQueue?.TryEnqueue(() => HardwareInfo = initialHardwareInfo);
    });
    
    // Set up timer for regular updates (every 5 seconds for UI, every 30 seconds for server)
    _hardwareUpdateTimer.Interval = TimeSpan.FromSeconds(5);
    _hardwareUpdateTimer.Tick += (sender, e) => {
        LoadHardwareInfo();      // Update UI
        SendHardwareUpdate();    // Send to server
    };
    _hardwareUpdateTimer.Start();
}
```

#### 12.5.2 WebSocket Message Handling
```csharp
// WebSocket message processing
_webSocketService.MessageReceived += (sender, s) =>
{
    var json = JsonConvert.DeserializeObject<WSMessage>(s);
    
    if (json.MessageType == "info")
    {
        if (json.Message == "registered")
        {
            _dispatcherQueue?.TryEnqueue(() =>
            {
                Connected = true;
                Growl.SuccessGlobal("Success", "Registered machine to the server!");
            });
        }
    }
    else if (json.MessageType == "command")
    {
        if (json.Message == "shutdown")
        {
            HandleShutdownCommand();
        }
    }
};
```

### 12.6 Remote Control Commands

#### 12.6.1 Shutdown Command Implementation
```csharp
private void HandleShutdownCommand()
{
    _dispatcherQueue?.TryEnqueue(() =>
    {
        if (Settings.RealShutDown)
        {
            // Execute real shutdown command
            Process.Start(new ProcessStartInfo
            {
                FileName = "shutdown",
                Arguments = $"-s -t -f 90",  // Shutdown in 90 seconds
                CreateNoWindow = true,
                UseShellExecute = false
            });
            
            Growl.WarningGlobal("Shutting down", "Your machine will shut down after 90 seconds!");
            MessageBox.ShowWarning(App.MainWindow, "Your machine will shut down after 90 seconds!", "Shutting down");
        }
        else
        {
            // Fake shutdown for testing
            Growl.WarningGlobal("Shutting down", "Trigger fake shut down!");
        }
        
        Connected = false;
    });
}
```

#### 12.6.2 Disconnect Command
```csharp
[RelayCommand]
public async Task Disconnect()
{
    try
    {
        await _webSocketService.ConnectAsync(new Uri(Settings.WebSocketUrl), CancellationToken.None);
        
        var disconnectMessage = new DisconnectMessage
        {
            messageType = "disconnect",
            macAddress = MacAddress
        };
        
        await _webSocketService.SendMessageAsync(disconnectMessage);
    }
    catch (Exception ex)
    {
        Debug.WriteLine(ex.Message);
    }
}
```

### 12.7 User Interface - DevWinUI Components

#### 12.7.1 Modern Fluent Design UI
```xml
<!-- Bento Card Style với Fluent Design -->
<Style x:Key="BentoCardStyle" TargetType="Border">
    <Setter Property="CornerRadius" Value="16"/>
    <Setter Property="Background" Value="{ThemeResource CardBackgroundFillColorDefaultBrush}"/>
    <Setter Property="BorderBrush" Value="{ThemeResource CardStrokeColorDefaultBrush}"/>
    <Setter Property="BorderThickness" Value="1"/>
    <Setter Property="Padding" Value="20"/>
    <Setter Property="Margin" Value="6"/>
</Style>
```

#### 12.7.2 Hardware Information Display
```xml
<!-- CPU Information Card -->
<Border Grid.Column="0" Style="{StaticResource BentoCardStyle}">
    <StackPanel>
        <StackPanel Orientation="Horizontal" Margin="0,0,0,12">
            <FontIcon Glyph="&#xE950;" FontSize="18" Margin="0,0,8,0" 
                      Foreground="{ThemeResource AccentTextFillColorPrimaryBrush}"/>
            <TextBlock Text="CPU" Style="{StaticResource CardTitleStyle}"/>
        </StackPanel>
        <TextBlock Text="{x:Bind ViewModel.HardwareInfo.cpu.model, Mode=OneWay}" 
                   Style="{StaticResource InfoValueStyle}" TextWrapping="Wrap"/>
        <StackPanel Spacing="4">
            <StackPanel Orientation="Horizontal">
                <TextBlock Text="Cores:" Width="50"/>
                <TextBlock Text="{x:Bind ViewModel.HardwareInfo.cpu.cores, Mode=OneWay}"/>
            </StackPanel>
            <StackPanel Orientation="Horizontal">
                <TextBlock Text="Usage:" Width="50"/>
                <TextBlock>
                    <Run Text="{x:Bind ViewModel.HardwareInfo.cpu.usage, Mode=OneWay}"/>
                    <Run Text=" %"/>
                </TextBlock>
            </StackPanel>
        </StackPanel>
    </StackPanel>
</Border>
```

#### 12.7.3 DevWinUI Settings Cards
```xml
<!-- Settings với DevWinUI SettingsCard -->
<dev:SettingsCard
    Description="By activating this option, clicking the close button will minimize the app window to a taskbar icon."
    Header="Close button should minimize the App window"
    HeaderIcon="{dev:BitmapIcon Source=Assets/Fluent/CloseTray.png}">
    <ToggleSwitch 
        IsOn="{x:Bind helper:AppHelper.Settings.UseTrayIcon, Mode=TwoWay, UpdateSourceTrigger=PropertyChanged}"/>
</dev:SettingsCard>

<dev:SettingsCard
    Description="By activating this option, the shutdown command will be call instead of a fake notification."
    Header="Real shutdown"
    HeaderIcon="{dev:BitmapIcon Source=Assets/Fluent/DevMode.png}">
    <ToggleSwitch 
        IsOn="{x:Bind helper:AppHelper.Settings.RealShutDown, Mode=TwoWay, UpdateSourceTrigger=PropertyChanged}"/>
</dev:SettingsCard>
```

### 12.8 System Tray Integration

#### 12.8.1 TaskbarIcon Implementation
```csharp
// H.NotifyIcon.WinUI tray icon integration
public void InitializeTrayIcon()
{
    if (!Settings.UseTrayIcon)
        return;

    HandleClosedEvents = Settings.UseTrayIcon;
    var showHideWindowCommand = (XamlUICommand)Resources["ShowHideWindowCommand"];
    showHideWindowCommand.ExecuteRequested += ShowHideWindowCommand_ExecuteRequested;
    var exitApplicationCommand = (XamlUICommand)Resources["ExitApplicationCommand"];
    exitApplicationCommand.ExecuteRequested += ExitApplicationCommand_ExecuteRequested;
    TrayIcon = (TaskbarIcon)Resources["TrayIcon"];
    TrayIcon.ForceCreate();
}

// Window management với tray icon
MainWindow.Closed += async (sender, args) =>
{
    if (HandleClosedEvents && Settings.Token != null)
    {
        args.Handled = true;     // Prevent closing
        MainWindow.Hide();       // Hide to tray instead
    }
    else
    {
        TrayIcon?.Dispose();
        Environment.Exit(0);
    }
};
```

### 12.9 Authentication Integration

#### 12.9.1 Better Auth Client Integration
```csharp
public class AuthService : IAuthService
{
    private readonly IHttpService _httpService;

    public async Task<bool> ValidateToken()
    {
        if (Settings.Token == null)
            return false;
            
        try
        {
            _httpService.SetBearerToken(Settings.Token);
            if(await GetSessionContext() != null)
                return true;

            Settings.Token = null;
            Settings.UserId = null;
            return false;
        }
        catch (Exception exception)
        {
            Debug.WriteLine(exception.Message);
            return false;
        }
    }

    public async Task<SessionContext?> GetSessionContext()
    {
        try
        {
            _httpService.SetBearerToken(Settings.Token);
            return await _httpService.GetAsync<SessionContext>("/auth/get-session");
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Error getting session context: {ex.Message}");
            return null;
        }
    }
}
```

#### 12.9.2 Auto-login Flow
```csharp
// Application startup với auto-login
public async void InitializeMainWindow()
{
    bool loggedIn = await AuthService.ValidateToken();
    Debug.WriteLine(loggedIn);
    MainWindow = new MainWindow(loggedIn);
    
    // Initialize window properties
    MainWindow.Title = MainWindow.AppWindow.Title = ProcessInfoHelper.ProductNameAndVersion;
    MainWindow.AppWindow.SetIcon("Assets/AppIcon.ico");
    
    InitializeTheme();
    InitializeTrayIcon();
    MainWindow.Activate();
}
```

### 12.10 Dependencies & Package Management

#### 12.10.1 NuGet Package Dependencies
```xml
<!-- Core WinUI 3 Framework -->
<PackageReference Include="Microsoft.WindowsAppSDK" Version="1.7.250401001" />
<PackageReference Include="WinUIEx" Version="2.5.1" />

<!-- MVVM Framework -->
<PackageReference Include="CommunityToolkit.Mvvm" Version="8.4.0" />
<PackageReference Include="CommunityToolkit.WinUI.Behaviors" Version="8.2.250402" />
<PackageReference Include="CommunityToolkit.WinUI.Media" Version="8.2.250402" />

<!-- DevWinUI Modern Controls -->
<PackageReference Include="DevWinUI" Version="8.2.0" />
<PackageReference Include="DevWinUI.Controls" Version="8.2.0" />

<!-- Hardware Information -->
<PackageReference Include="Hardware.Info" Version="100.1.1" />
<PackageReference Include="System.Management" Version="9.0.0" />
<PackageReference Include="System.Diagnostics.PerformanceCounter" Version="9.0.1" />

<!-- System Tray Integration -->
<PackageReference Include="H.NotifyIcon.WinUI" Version="2.3.0" />

<!-- Configuration Management -->
<PackageReference Include="nucs.JsonSettings" Version="2.0.2" />
<PackageReference Include="nucs.JsonSettings.AutoSaveGenerator" Version="2.0.4" />

<!-- Dependency Injection -->
<PackageReference Include="Microsoft.Extensions.DependencyInjection" Version="9.0.4" />

<!-- XAML Behaviors -->
<PackageReference Include="Microsoft.Xaml.Behaviors.WinUI.Managed" Version="3.0.0" />
```

#### 12.10.2 Target Framework Configuration
```xml
<PropertyGroup>
    <OutputType>WinExe</OutputType>
    <TargetFramework>net9.0-windows10.0.26100.0</TargetFramework>
    <TargetPlatformMinVersion>10.0.17763.0</TargetPlatformMinVersion>
    <UseWinUI>true</UseWinUI>
    <EnableMsixTooling>true</EnableMsixTooling>
    <WindowsAppSDKSelfContained>true</WindowsAppSDKSelfContained>
    <Platforms>x64</Platforms>
    <RuntimeIdentifiers>win-x64</RuntimeIdentifiers>
</PropertyGroup>
```

### 12.11 Deployment & Distribution

#### 12.11.1 MSIX Packaging
```xml
<!-- MSIX Package Configuration -->
<PropertyGroup>
    <WindowsPackageType>None</WindowsPackageType>  <!-- Unpackaged deployment -->
    <ApplicationIcon>Assets\AppIcon.ico</ApplicationIcon>
    <Version>1.0.0</Version>
    <PackageCertificateThumbprint>C1F7F02C0E09DEE06830AEE5CB34846C8D8E2E9D</PackageCertificateThumbprint>
    <AppxPackageSigningEnabled>True</AppxPackageSigningEnabled>
</PropertyGroup>
```

#### 12.11.2 Publish Configuration
```xml
<!-- Publish Properties -->
<PropertyGroup>
    <PublishAot>False</PublishAot>
    <PublishReadyToRun Condition="'$(Configuration)' != 'Debug'">True</PublishReadyToRun>
    <PublishTrimmed Condition="'$(Configuration)' != 'Debug'">True</PublishTrimmed>
</PropertyGroup>
```

#### 12.11.3 Self-Contained Deployment
- **Framework**: .NET 9.0 với Windows Runtime support
- **Target**: Windows 10 version 1809 (build 17763) trở lên
- **Architecture**: x64 only
- **Distribution**: Self-contained executable với Windows App SDK

### 12.12 Error Handling & Reliability

#### 12.12.1 Connection Management
```csharp
// Auto-reconnection logic
private async Task<bool> EnsureWebSocketConnection()
{
    if (_webSocketService == null || !Connected)
    {
        try
        {
            await _webSocketService.ConnectAsync(new Uri(Settings.WebSocketUrl), CancellationToken.None);
            return true;
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"WebSocket connection failed: {ex.Message}");
            return false;
        }
    }
    return true;
}
```

#### 12.12.2 Hardware Collection Fallbacks
```csharp
// Comprehensive error handling với fallback methods
public async Task<HardwareInfo> GetHardwareInfoAsync()
{
    var hardwareInfo = new HardwareInfo();

    try
    {
        // Parallel processing for faster data collection
        var tasks = new Task[] { /* hardware collection tasks */ };
        await Task.WhenAll(tasks).ConfigureAwait(false);
    }
    catch (Exception ex)
    {
        Debug.WriteLine($"Error collecting hardware info: {ex.Message}");
        // Use fallback methods
        hardwareInfo.cpu = GetCpuInfoFallback();
        hardwareInfo.memory = GetMemoryInfoFallback();
        hardwareInfo.storage = GetStorageInfoFallback();
        // ... other fallbacks
    }

    return hardwareInfo;
}
```

#### 12.12.3 Performance Counter Management
```csharp
// Safe performance counter cleanup
public static void Cleanup()
{
    lock (_cpuCounterLock)
    {
        _cpuCounter?.Dispose();
        _cpuCounter = null;
    }
}
```

### 12.13 Security Measures

#### 12.13.1 Configuration Security
- **Token Storage**: Secure storage trong AppData folder
- **Auto-save Configuration**: `nucs.JsonSettings` với automatic encryption
- **Process Isolation**: Separate process space cho hardware monitoring

#### 12.13.2 Command Validation
```csharp
// Shutdown command validation
if (Settings.RealShutDown)
{
    // Only execute if explicitly enabled in settings
    Process.Start(new ProcessStartInfo
    {
        FileName = "shutdown",
        Arguments = $"-s -t -f 90",  // Force shutdown with 90-second delay
        CreateNoWindow = true,       // Hidden process
        UseShellExecute = false      // Direct process execution
    });
}
```

### 12.14 Performance Optimizations

#### 12.14.1 UI Thread Management
```csharp
// DispatcherQueue for UI updates
private readonly DispatcherQueue _dispatcherQueue = DispatcherQueue.GetForCurrentThread();

// Non-blocking UI updates
_dispatcherQueue?.TryEnqueue(() =>
{
    HardwareInfo = newHardwareInfo;
    LastUpdateTime = DateTime.Now.ToString("HH:mm:ss");
});
```

#### 12.14.2 Resource Management
```csharp
// Timer management
public void StopHardwareUpdates()
{
    _hardwareUpdateTimer?.Stop();
    _hardwareUpdateTimer = null;
    HardwareInfoService.Cleanup();  // Clean up performance counters
}
```

### 12.15 Development Tools & Debugging

#### 12.15.1 Debug Output
```csharp
// Comprehensive debug logging
Debug.WriteLine($"websocket: {json}");
Debug.WriteLine($"Error collecting hardware info: {ex.Message}");
Debug.WriteLine(JsonConvert.SerializeObject(addresses.Select(e => e.ToString())));
```

#### 12.15.2 Live Hardware Monitoring
- **Real-time Updates**: 5-second refresh interval cho UI
- **Server Updates**: 30-second interval cho server communication
- **Performance Metrics**: CPU, Memory, Storage, GPU usage tracking
- **Network Monitoring**: Interface status và bandwidth tracking

---

## 13. TÍCH HỢP GIỮA WINUI CLIENT VÀ WEB SERVER

### 13.1 End-to-End Communication Flow

```
┌─────────────────┐    WebSocket    ┌──────────────────┐    Socket.IO    ┌─────────────────┐
│   WinUI Client  │ ◄────────────► │   NuxtJS Server │ ◄────────────► │   Web Dashboard │
│  (Device Agent) │                │  (Node.js API)  │                │  (Admin Panel)  │
└─────────────────┘                └──────────────────┘                └─────────────────┘
         │                                   │                                   │
         │ 1. Device Registration            │ 4. Store in Database              │
         │ 2. Hardware Updates               │ 5. Broadcast to Web Clients      │ 7. Real-time Updates
         │ 3. Command Responses              │ 6. Admin Commands                │ 8. Device Control
         │                                   │                                   │
         └─────────── Network Protocols ─────┴──────── Network Protocols ───────┘
           - WebSocket (RFC 6455)                      - Socket.IO Protocol
           - JSON Message Format                       - HTTP/HTTPS RESTful API
           - Auto-reconnection                         - Session-based Auth
```

### 13.2 Message Protocol Compatibility

#### 13.2.1 Device Registration Flow
```javascript
// WinUI Client → Server (WebSocket)
{
  "messageType": "register",
  "userId": "auth-user-id-from-better-auth",
  "macAddress": "00:11:22:33:44:55",
  "ipAddress": "192.168.1.100", 
  "machineName": "DESKTOP-ABC123",
  "hardware": {
    "cpu": { "model": "Intel Core i7-12700K", "cores": 12, "speed": 3.6, "usage": 25.5 },
    "memory": { "total": 32, "used": 16.5, "available": 15.5, "usage": 51.6 },
    "storage": { "total": 1000, "used": 450, "available": 550, "usage": 45.0 },
    "gpu": { "model": "NVIDIA RTX 4070", "memory": 12, "usage": 15.2 },
    "network": { "interfaces": [{"name": "Ethernet", "type": "ethernet", "speed": 1000}] },
    "os": { "name": "Windows", "version": "11", "architecture": "x64", "uptime": 86400 },
    "motherboard": { "manufacturer": "ASUS", "model": "ROG STRIX Z690-E" }
  }
}

// Server Response → WinUI Client (WebSocket)
{
  "MessageType": "info",
  "Message": "registered"
}

// Server → Web Dashboard (Socket.IO)
{
  "event": "device-update",
  "data": {
    "macAddress": "00:11:22:33:44:55",
    "userId": "auth-user-id-from-better-auth",
    "name": "DESKTOP-ABC123",
    "ipAddress": "192.168.1.100",
    "isConnected": true,
    "hardware": { /* same hardware object */ },
    "lastSeen": "2025-01-26T10:30:00Z",
    "userName": "John Doe",
    "userEmail": "john@example.com"
  }
}
```

#### 13.2.2 Remote Shutdown Command Flow
```javascript
// Web Dashboard → Server (Socket.IO)
{
  "event": "shutdown-request", 
  "data": {
    "macAddress": "00:11:22:33:44:55"
  }
}

// Server → WinUI Client (WebSocket)
{
  "MessageType": "command",
  "Message": "shutdown"
}

// WinUI Client Processing (C#)
if (json.MessageType == "command" && json.Message == "shutdown")
{
    if (Settings.RealShutDown)
    {
        Process.Start(new ProcessStartInfo
        {
            FileName = "shutdown",
            Arguments = $"-s -t -f 90",
            CreateNoWindow = true,
            UseShellExecute = false
        });
    }
}

// Server → Web Dashboard (Socket.IO)
{
  "event": "device-shutdown",
  "data": {
    "macAddress": "00:11:22:33:44:55",
    "success": true
  }
}
```

### 13.3 Kiến trúc Authentication Tích hợp

#### 13.3.1 Better Auth Integration
```
┌─────────────────┐                 ┌──────────────────┐                 ┌─────────────────┐
│   WinUI Client  │                 │   NuxtJS Server │                 │   Web Dashboard │
│                 │                 │   Better Auth   │                 │                 │
├─────────────────┤                 ├──────────────────┤                 ├─────────────────┤
│ 1. Login Form   │ ──── HTTP ────► │ POST /auth/      │ ◄──── HTTP ──── │ 1. Admin Login  │
│ 2. Store Token  │ ◄─── Response ─ │    sign-in       │ ───► Response ─► │ 2. Session      │
│ 3. WebSocket    │                 │ 3. Validate      │                 │ 3. Socket.IO    │
│    Connection   │ ──── WS + JWT ─► │    Session       │ ◄─── Socket ──── │    Connection   │
│ 4. Auto-reconnect│                │ 4. User Role     │                 │ 4. Role-based   │
│                 │                 │    (admin/user)  │                 │    UI           │
└─────────────────┘                 └──────────────────┘                 └─────────────────┘

// Shared Authentication Flow:
// 1. Both WinUI và Web clients sử dụng Better Auth
// 2. Session-based authentication với PostgreSQL
// 3. Role-based authorization (admin có thể xem all devices)
// 4. Auto token validation và refresh
```

#### 13.3.2 User Permission Matrix
```
┌──────────────┬─────────────────┬─────────────────┬─────────────────┐
│ Action       │ Regular User    │ Admin User      │ WinUI Client    │
├──────────────┼─────────────────┼─────────────────┼─────────────────┤
│ View Devices │ Own devices     │ All devices     │ Register own    │
│ Shutdown     │ Own devices     │ Any device      │ Receive command │
│ User Filter  │ Not available   │ Available       │ Not applicable  │
│ Device List  │ /devices        │ /admin/devices  │ WebSocket only  │
│ Hardware     │ View only       │ View only       │ Send updates    │
└──────────────┴─────────────────┴─────────────────┴─────────────────┘
```

### 13.4 Database Integration

#### 13.4.1 Shared Data Models
```sql
-- Device table được sử dụng bởi cả WinUI client và Web dashboard
CREATE TABLE devices (
    mac_address VARCHAR(17) PRIMARY KEY,           -- From WinUI client
    user_id TEXT NOT NULL REFERENCES user(id),     -- Better Auth user ID
    name VARCHAR(100) NOT NULL,                     -- Environment.MachineName
    ip_address VARCHAR(45) NOT NULL,               -- Network detection
    is_connected BOOLEAN DEFAULT FALSE,            -- WebSocket status
    hardware JSONB,                                -- HardwareInfo object
    last_seen TIMESTAMP DEFAULT NOW(),             -- Last update time
    created_at TIMESTAMP DEFAULT NOW(),
    updated_at TIMESTAMP DEFAULT NOW()
);

-- User table được quản lý bởi Better Auth
CREATE TABLE user (
    id TEXT PRIMARY KEY,                           -- Better Auth ID
    name TEXT NOT NULL,                            -- Display name
    email TEXT UNIQUE NOT NULL,                    -- Login email
    role TEXT DEFAULT 'user',                      -- 'user' | 'admin'
    created_at TIMESTAMP NOT NULL,
    updated_at TIMESTAMP NOT NULL
);
```

#### 13.4.2 Real-time Data Synchronization
```
┌─────────────────┐    1. Hardware   ┌──────────────────┐    3. Socket.IO   ┌─────────────────┐
│   WinUI Client  │ ───► Update  ───► │   PostgreSQL     │ ───► Broadcast ──► │   Web Dashboard │
│  (Every 5s UI   │                  │   + Drizzle ORM  │                   │  (Live Updates) │
│   Every 30s WS) │ ◄─── Stored ─────┤                  │ ◄─── Query ────── │                 │
└─────────────────┘    2. Database    └──────────────────┘    4. Admin View  └─────────────────┘

// Update Flow:
// 1. WinUI client gửi UpdateMessage qua WebSocket
// 2. Server cập nhật PostgreSQL database
// 3. Server broadcast device-update event qua Socket.IO  
// 4. Web dashboard nhận real-time updates
// 5. Admin có thể xem devices của tất cả users
```

### 13.5 Scalability & Performance

#### 13.5.1 Connection Management
```
Multiple WinUI Clients ──┐
                        │
Windows Device Agent 1 ──┤    WebSocket        ┌─── Socket.IO ──► Web Client 1 (User)
Windows Device Agent 2 ──┼──► Connections  ───► │                 Web Client 2 (Admin)
Windows Device Agent N ──┤    Pool             │                 Web Client 3 (Admin)
                        │                       │
Hardware Updates (5s) ───┘                     └─── HTTP API ───► Mobile App (Future)

// Performance Considerations:
// - WebSocket connection pooling cho multiple devices
// - Socket.IO singleton pattern để tránh duplicate events
// - Database connection pooling với Drizzle ORM
// - Hardware data caching (5 minutes for static info)
// - Rate limiting cho hardware updates
```

#### 13.5.2 Deployment Architecture  
```
┌─────────────────────────────────────────────────────────────────────────┐
│                          Production Environment                          │
├─────────────────────────────────────────────────────────────────────────┤
│                                                                         │
│  ┌─────────────────┐     ┌──────────────────┐     ┌─────────────────┐  │
│  │ WinUI Clients   │     │   NuxtJS Server  │     │  PostgreSQL DB  │  │
│  │ (Self-deployed) │────►│  (Vercel/VPS)    │────►│  (Cloud/Local)  │  │
│  │                 │ WS  │                  │ ORM │                 │  │
│  │ - Auto-start    │     │ - WebSocket      │     │ - JSONB storage │  │
│  │ - System tray   │     │ - Socket.IO      │     │ - Indexing      │  │
│  │ - Auto-update   │     │ - Better Auth    │     │ - Backup        │  │
│  └─────────────────┘     └──────────────────┘     └─────────────────┘  │
│           │                        │                        │          │
│           │                        │                        │          │
│  ┌─────────────────┐     ┌──────────────────┐     ┌─────────────────┐  │
│  │ Corporate       │     │   Load Balancer  │     │    Monitoring   │  │
│  │ Network/VPN     │     │   (Optional)     │     │    & Logging    │  │
│  │                 │     │                  │     │                 │  │
│  │ - Multiple PCs  │     │ - SSL/TLS        │     │ - Device status │  │
│  │ - Domain auth   │     │ - Rate limiting  │     │ - Performance   │  │
│  │ - Firewall      │     │ - Health checks  │     │ - Error logs    │  │
│  └─────────────────┘     └──────────────────┘     └─────────────────┘  │
└─────────────────────────────────────────────────────────────────────────┘
```

---

## 14. TESTING & VALIDATION INTEGRATION

### 14.1 End-to-End Testing Scenarios

#### 14.1.1 Device Registration Testing
```csharp
// WinUI Client Test
[Test]
public async Task TestDeviceRegistration()
{
    // Arrange
    var webSocketService = new WebSocketService();
    var hardwareService = new HardwareInfoService();
    
    // Act
    await webSocketService.ConnectAsync(new Uri("ws://localhost:3000/api/websocket"));
    var registerMessage = new RegisterMessage
    {
        messageType = "register",
        userId = "test-user-id",
        macAddress = "00:11:22:33:44:55",
        ipAddress = "192.168.1.100",
        machineName = "TEST-MACHINE",
        hardware = await hardwareService.GetHardwareInfoAsync()
    };
    await webSocketService.SendMessageAsync(registerMessage);
    
    // Assert - Check if device appears in web dashboard
    // Verify database entry
    // Confirm Socket.IO broadcast
}
```

#### 14.1.2 Cross-Platform Communication Test
```javascript
// Server-side integration test
describe('WinUI Client Integration', () => {
  it('should handle device registration and broadcast to web clients', async () => {
    // 1. Simulate WinUI client connection
    const wsClient = new WebSocket('ws://localhost:3000/api/websocket');
    
    // 2. Setup Socket.IO listener cho web client
    const socketIOClient = io('http://localhost:3000');
    let deviceUpdateReceived = false;
    
    socketIOClient.on('device-update', (data) => {
      deviceUpdateReceived = true;
      expect(data.macAddress).toBe('00:11:22:33:44:55');
    });
    
    // 3. Send registration từ WinUI client
    wsClient.send(JSON.stringify({
      messageType: "register",
      macAddress: "00:11:22:33:44:55",
      // ... other fields
    }));
    
    // 4. Verify web client receives update
    await new Promise(resolve => setTimeout(resolve, 1000));
    expect(deviceUpdateReceived).toBe(true);
  });
});
```

### 14.2 Performance Benchmarking

#### 14.2.1 Hardware Collection Performance
```csharp
// WinUI Client Performance Test
[Benchmark]
public async Task<HardwareInfo> GetHardwareInfoBenchmark()
{
    var service = new HardwareInfoService();
    return await service.GetHardwareInfoAsync();
}

// Expected Results:
// - Parallel collection: ~200-500ms
// - Sequential collection: ~1-2 seconds  
// - Cached static info: ~50-100ms
// - Fallback methods: ~100-200ms
```

#### 14.2.2 WebSocket Throughput Testing
```csharp
// Connection stress test
[Test]
public async Task TestMultipleDeviceConnections()
{
    const int deviceCount = 100;
    var tasks = new List<Task>();
    
    for (int i = 0; i < deviceCount; i++)
    {
        tasks.Add(ConnectAndRegisterDevice($"device-{i}"));
    }
    
    await Task.WhenAll(tasks);
    
    // Verify all devices are registered
    // Check server performance metrics
    // Validate Socket.IO broadcast performance
}
```

---

**Ngày hoàn thành:** 26/01/2025  
**Project Repository:** [GitHub Link]  
**Live Demo:** [Production URL]  
**WinUI Client Download:** [Release Link]