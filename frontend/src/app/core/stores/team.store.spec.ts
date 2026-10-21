import { TestBed } from '@angular/core/testing';
import { HttpClientTestingModule, HttpTestingController } from '@angular/common/http/testing';
import { TeamStore } from './team.store';
import { MOCK_STAFF_MEMBERS } from '../mocks/f4-mock-data';
import { CreateStaffMemberRequest } from '../models/team-settings.models';

describe('TeamStore', () => {
  let store: TeamStore;
  let httpMock: HttpTestingController;

  beforeEach(() => {
    TestBed.configureTestingModule({
      imports: [HttpClientTestingModule],
      providers: [TeamStore]
    });

    store = TestBed.inject(TeamStore);
    httpMock = TestBed.inject(HttpTestingController);
  });

  afterEach(() => {
    httpMock.verify();
  });

  it('should initialize with default values', () => {
    expect(store.members()).toEqual([]);
    expect(store.maxStaffQuota()).toBe(8);
    expect(store.roleFilter()).toBe('all');
    expect(store.isLoading()).toBeFalse();
  });

  it('should load members and compute current count and quota status', async () => {
    const loadPromise = store.loadMembers();
    const req = httpMock.expectOne('/api/v1/workspaces/ws-main/members');
    expect(req.request.method).toBe('GET');
    req.flush(MOCK_STAFF_MEMBERS);

    await loadPromise;
    expect(store.members().length).toBe(MOCK_STAFF_MEMBERS.length);
    expect(store.currentMemberCount()).toBe(MOCK_STAFF_MEMBERS.length);
    expect(store.isQuotaReached()).toBeFalse();
  });

  it('should filter members by role', () => {
    store.members.set(MOCK_STAFF_MEMBERS);
    store.roleFilter.set('Teacher');
    expect(store.filteredMembers().every(m => m.role === 'Teacher')).toBeTrue();
    expect(store.filteredMembers().length).toBe(1);
  });

  it('should add a member when under quota', async () => {
    store.members.set(MOCK_STAFF_MEMBERS);
    const newReq: CreateStaffMemberRequest = {
      displayName: 'منى حامد',
      username: 'mona.reception',
      email: 'mona@tahdeera.eg',
      phone: '01099887766',
      role: 'Reception',
      password: 'StrongPassword123!'
    };

    const addPromise = store.addMember(newReq);
    const req = httpMock.expectOne('/api/v1/workspaces/ws-main/members');
    expect(req.request.method).toBe('POST');
    expect(req.request.body).toEqual(newReq);

    const created = {
      id: 'staff-new',
      name: newReq.displayName,
      displayName: newReq.displayName,
      username: newReq.username,
      email: newReq.email,
      phone: newReq.phone,
      role: newReq.role,
      isActive: true,
      status: 'Active' as const,
      createdAt: '2026-10-21T10:00:00Z',
      createdAtUtc: '2026-10-21T10:00:00Z',
      initialPasswordCleartext: newReq.password
    };
    req.flush(created);

    const res = await addPromise;
    expect(res.id).toBe('staff-new');
    expect(store.latestCreatedMember()?.displayName).toBe('منى حامد');
    expect(store.members().some(m => m.id === 'staff-new')).toBeTrue();
  });

  it('should block adding member if quota is reached', async () => {
    store.maxStaffQuota.set(2);
    store.members.set([MOCK_STAFF_MEMBERS[0], MOCK_STAFF_MEMBERS[1]]);

    expect(store.isQuotaReached()).toBeTrue();

    const newReq: CreateStaffMemberRequest = {
      displayName: 'اختبار',
      username: 'test',
      email: 'test@tahdeera.eg',
      phone: '01011223344',
      role: 'Assistant',
      password: 'Pass'
    };

    await expectAsync(store.addMember(newReq)).toBeRejectedWithError(/QUOTA_MAX_STAFF_REACHED/);
  });

  it('should deactivate a member', async () => {
    store.members.set([...MOCK_STAFF_MEMBERS]);
    const targetId = MOCK_STAFF_MEMBERS[1].id;

    const deactPromise = store.deactivateMember(targetId);
    const req = httpMock.expectOne(`/api/v1/workspaces/ws-main/members/${targetId}/deactivate`);
    expect(req.request.method).toBe('POST');

    const deactivated = { ...MOCK_STAFF_MEMBERS[1], status: 'Deactivated' as const };
    req.flush(deactivated);

    await deactPromise;
    const member = store.members().find(m => m.id === targetId);
    expect(member?.status).toBe('Deactivated');
  });
});
