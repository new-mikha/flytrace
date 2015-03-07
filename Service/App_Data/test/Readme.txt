Files like those in sample_test_feeds.zip can be used by
/Tracker/service/test/TestControl.aspx to be displayed as separate trackers on
test group=1

Each XML file not starting from '_' considered as a SPOT feed of a separate
tracker in group=1, with tracker name == file name. Messages from the feeds
delivered one by one in according to the controls on the page above. Initially
it's "all feeds empty", with current position #1 set on the page the earlies
one "delivered" to the page, with position #2 the second earliest message
delivered too and so on.

_empty.xml is needed, leave it as it is.

IT TAKES TIME TO DELIVER NEW POSTIONS FROM FEEDS - the time is needed for
TrackerDataManager to refresh the feed, so few seconds should pass. Be patient
and don't press Update button expecting that a new position will come through
immediately after the change of the POsition value on the control page.

THE TEST CONTROL PAGE IS SUPPOSED TO BE ACCESSIBLE FOR ADMINS ONLY. 
See web.config for detail

IT IS DEV TESTING FUNCTIONALITY, not very mature, easy to break - e.g.
decreasing Position without restarting the service can have bad consequences
for the test map (with group=1)