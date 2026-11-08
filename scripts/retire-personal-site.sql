-- Run once per database BEFORE applying the RetirePersonalSite migration,
-- which drops show_on_agency and contact_submissions.site.
-- Personal-only rows (shown on personal, not agency) go to the admin trash; drafts with neither flag stay.

begin;

update projects set is_deleted = true, deleted_at = now(), updated_at = now()
where show_on_personal = true and show_on_agency = false and is_deleted = false;

update products set is_deleted = true, deleted_at = now(), updated_at = now()
where show_on_personal = true and show_on_agency = false and is_deleted = false;

update articles set is_deleted = true, deleted_at = now(), updated_at = now()
where show_on_personal = true and show_on_agency = false and is_deleted = false;

update services set is_deleted = true, deleted_at = now(), updated_at = now()
where show_on_personal = true and show_on_agency = false and is_deleted = false;

update faqs set is_deleted = true, deleted_at = now(), updated_at = now()
where show_on_personal = true and show_on_agency = false and is_deleted = false;

update contact_submissions set is_deleted = true, deleted_at = now(), updated_at = now()
where site = 'personal' and is_deleted = false;

commit;
