import { useUsers, useUpdateUserRole, useDeactivateUser } from '../hooks/useUsers'
import StatusChip from '../components/ui/StatusChip'

const ALL_ROLES = [
  'Administrator', 'Operator', 'Supervisor', 'Auditor',
  'SecurityOfficer', 'Student',
]

export default function UsersPage() {
  const { data, isLoading } = useUsers()
  const updateRole = useUpdateUserRole()
  const deactivate = useDeactivateUser()

  if (isLoading) return <p>Loading...</p>

  const users = data?.users ?? []

  return (
    <div className="p-6 space-y-4">
      <h1 className="text-2xl font-bold">Users</h1>

      {users.length === 0 ? (
        <p className="text-muted-foreground">No users found.</p>
      ) : (
        <table className="w-full text-sm border-collapse">
          <thead>
            <tr className="border-b text-left">
              <th className="py-2 pr-4">Email</th>
              <th className="py-2 pr-4">Role</th>
              <th className="py-2 pr-4">Status</th>
              <th className="py-2 pr-4">Created</th>
              <th className="py-2" />
            </tr>
          </thead>
          <tbody>
            {users.map(user => (
              <tr key={user.userId} className="border-b hover:bg-muted/30">
                <td className="py-2 pr-4">{user.email}</td>
                <td className="py-2 pr-4">
                  <select
                    value={user.role}
                    onChange={e =>
                      updateRole.mutate({ userId: user.userId, role: e.target.value })
                    }
                    className="rounded border px-2 py-1 text-xs bg-background"
                    disabled={updateRole.isPending}
                  >
                    {ALL_ROLES.map(r => (
                      <option key={r} value={r}>{r}</option>
                    ))}
                  </select>
                </td>
                <td className="py-2 pr-4">
                  <StatusChip
                    variant={user.isActive ? 'success' : 'muted'}
                    label={user.isActive ? 'Active' : 'Inactive'}
                  />
                </td>
                <td className="py-2 pr-4 text-muted-foreground">
                  {new Date(user.createdAt).toLocaleDateString()}
                </td>
                <td className="py-2">
                  {user.isActive && (
                    <button
                      onClick={() => deactivate.mutate(user.userId)}
                      disabled={deactivate.isPending}
                      className="px-3 py-1 text-xs rounded border border-red-500 text-red-500 hover:bg-red-500/10 disabled:opacity-50"
                    >
                      Deactivate
                    </button>
                  )}
                </td>
              </tr>
            ))}
          </tbody>
        </table>
      )}
    </div>
  )
}
