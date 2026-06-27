import { useRoles } from '../hooks/useRoles'

const PERMISSION_GROUPS: Record<string, string[]> = {
  'Capture': ['capture.read', 'capture.write', 'upload.write'],
  'Exams':   ['exams.read', 'exams.write'],
  'OCR':     ['ocr.read', 'ocr.write', 'review.write'],
  'Scoring': ['score.read', 'score.write', 'result.read'],
  'Users':   ['users.read', 'users.write', 'users.manage'],
  'Devices': ['devices.read', 'devices.manage'],
  'Audit':   ['audit.read', 'security.read'],
}

const ALL_PERMS = Object.values(PERMISSION_GROUPS).flat()

export default function RolesPage() {
  const { data, isLoading } = useRoles()

  if (isLoading) return <p>Loading...</p>

  const roles = data?.roles ?? []

  return (
    <div className="p-6 space-y-6">
      <div className="flex items-center gap-3">
        <h1 className="text-2xl font-bold">Roles &amp; Permissions</h1>
        <span className="text-sm text-muted-foreground">{roles.length} roles</span>
      </div>

      {/* Role cards */}
      <div className="grid grid-cols-1 gap-3 sm:grid-cols-2 lg:grid-cols-3">
        {roles.map(role => (
          <div
            key={role.roleName}
            className="rounded-lg border border-border bg-card p-4 space-y-2"
          >
            <div className="flex items-start justify-between">
              <span className="font-semibold text-foreground">{role.displayName}</span>
              <span className="text-xs text-muted-foreground whitespace-nowrap ml-2">
                {role.permissions.length} permissions
              </span>
            </div>
            <p className="text-xs text-muted-foreground">{role.description}</p>
            <div className="flex flex-wrap gap-1 pt-1">
              {role.permissions.map(p => (
                <span
                  key={p}
                  className="px-2 py-0.5 text-xs rounded-full bg-primary/10 text-primary border border-primary/20"
                >
                  {p}
                </span>
              ))}
            </div>
          </div>
        ))}
      </div>

      {/* Permission matrix */}
      <div className="overflow-x-auto rounded-lg border border-border">
        <table className="w-full text-xs border-collapse">
          <thead>
            <tr className="bg-muted/50">
              <th className="sticky left-0 bg-muted/80 px-4 py-3 text-left font-semibold text-foreground min-w-[140px]">
                Permission
              </th>
              {roles.map(r => (
                <th key={r.roleName} className="px-3 py-3 text-center font-semibold text-foreground min-w-[100px]">
                  {r.displayName}
                </th>
              ))}
            </tr>
          </thead>
          <tbody>
            {Object.entries(PERMISSION_GROUPS).map(([group, perms]) => (
              <>
                <tr key={`g-${group}`} className="bg-muted/20">
                  <td
                    colSpan={roles.length + 1}
                    className="px-4 py-1.5 text-[11px] font-semibold uppercase tracking-wider text-muted-foreground"
                  >
                    {group}
                  </td>
                </tr>
                {perms.map(perm => (
                  <tr key={perm} className="border-t border-border hover:bg-muted/10">
                    <td className="sticky left-0 bg-background px-4 py-2 font-mono text-muted-foreground">
                      {perm}
                    </td>
                    {roles.map(role => {
                      const granted = role.permissions.includes(perm)
                      return (
                        <td
                          key={role.roleName}
                          className="px-3 py-2 text-center"
                          data-granted={granted}
                        >
                          {granted ? (
                            <span className="inline-block w-4 h-4 rounded-full bg-green-500/20 border border-green-500/50 text-green-500 text-[10px] leading-4">
                              ✓
                            </span>
                          ) : (
                            <span className="inline-block w-4 h-4 text-muted-foreground/30 text-[10px] leading-4">
                              —
                            </span>
                          )}
                        </td>
                      )
                    })}
                  </tr>
                ))}
              </>
            ))}
          </tbody>
        </table>
      </div>
    </div>
  )
}
