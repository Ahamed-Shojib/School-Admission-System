import { useEffect } from 'react';
import { StudentIdBar } from './components/StudentIdBar';
import { useAdmin } from './hooks/useAdmin';
import { useHashRoute } from './hooks/useHashRoute';
import { useStudentId } from './hooks/useStudentId';
import { AdminLoginPage } from './pages/AdminLoginPage';
import { ClassesPage } from './pages/ClassesPage';
import { CreateClassPage } from './pages/CreateClassPage';
import { HoldDetailPage } from './pages/HoldDetailPage';
import { MyApplicationsPage } from './pages/MyApplicationsPage';

export default function App() {
  const { studentId, setStudentId, isValid } = useStudentId();
  const admin = useAdmin();
  const { path, navigate } = useHashRoute();

  const isAdminRoute = path === '/admin' || path === '/admin/login';

  // After logging in (or if already logged in), the login URL leads straight to the admin section.
  useEffect(() => {
    if (admin.isAdmin && path === '/admin/login') navigate('/admin');
  }, [admin.isAdmin, path, navigate]);

  const nav = (to: string, label: string, active: boolean) => (
    <a href={`#${to}`} className={active ? 'active' : undefined}>
      {label}
    </a>
  );

  let page;
  if (isAdminRoute) {
    page = admin.isAdmin ? (
      <CreateClassPage adminToken={admin.token} onSessionEnded={admin.logout} navigate={navigate} />
    ) : (
      <AdminLoginPage
        onLoggedIn={(session) => {
          admin.login(session);
          navigate('/admin');
        }}
      />
    );
  } else if (path.startsWith('/holds/')) {
    // key: opening another application starts a fresh page state
    page = (
      <HoldDetailPage
        key={path}
        holdId={decodeURIComponent(path.slice('/holds/'.length))}
        studentId={studentId}
        isValid={isValid}
        navigate={navigate}
      />
    );
  } else if (path === '/me') {
    page = <MyApplicationsPage studentId={studentId} isValid={isValid} navigate={navigate} />;
  } else {
    page = <ClassesPage studentId={studentId} isValid={isValid} navigate={navigate} />;
  }

  return (
    <div className="app">
      <header>
        <div className="topbar">
          <h1>School Admission System</h1>
          <div className="admin-actions">
            {admin.isAdmin ? (
              <>
                <a href="#/admin" className="btn-link">
                  Admin panel
                </a>
                <button
                  type="button"
                  className="secondary-outline"
                  onClick={() => {
                    admin.logout();
                    navigate('/classes');
                  }}
                >
                  Log out
                </button>
              </>
            ) : (
              <a href="#/admin/login" className="btn-link">
                Admin login
              </a>
            )}
          </div>
        </div>
        <StudentIdBar studentId={studentId} isValid={isValid} onChange={setStudentId} />
        <nav>
          {nav('/classes', 'Classes', !isAdminRoute && path !== '/me' && !path.startsWith('/holds/'))}
          {nav('/me', 'My applications', path === '/me')}
        </nav>
      </header>
      <main>{page}</main>
    </div>
  );
}
