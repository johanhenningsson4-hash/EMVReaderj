# CI/CD Workflow Documentation

This directory contains GitHub Actions workflows for the EMV Card Reader project.

## 📋 Available Workflows

### 1. **CI/CD Pipeline** (`ci-cd.yml`)
**Triggers:** Push to main/develop, Pull Requests, Manual releases

**Jobs:**
- **Build and Test** - Compiles solution and runs tests
- **Security Scan** - CodeQL security analysis 
- **Create Release** - Automatic releases on version bumps
- **Notify Teams** - Release notifications
- **Cleanup** - Artifact maintenance

**Features:**
- ✅ Automated building with MSBuild
- ✅ Unit test execution and reporting
- ✅ Artifact creation and storage
- ✅ Security scanning with CodeQL
- ✅ Automatic GitHub releases
- ✅ Version extraction from AssemblyInfo.cs

### 2. **Pull Request Validation** (`pr-validation.yml`)
**Triggers:** Pull request events

**Jobs:**
- **Validate PR** - Build validation and testing
- **PR Feedback** - Automated feedback comments

**Features:**
- ✅ Debug build validation
- ✅ Test result reporting
- ✅ Version change detection
- ✅ Code style validation
- ✅ Automated PR feedback

### 3. **Dependency Management** (`dependency-management.yml`)
**Triggers:** Weekly schedule (Sundays), Manual dispatch

**Jobs:**
- **Check Dependencies** - NuGet package auditing

**Features:**
- ✅ Weekly dependency scans
- ✅ Security vulnerability checks
- ✅ Automated issue creation
- ✅ Package update recommendations

## 🔧 Configuration

### Environment Variables
```yaml
SOLUTION_FILE: EMVReader.sln
BUILD_CONFIGURATION: Release
DOTNET_FRAMEWORK_VERSION: 4.7.2
```

### Required Secrets
- `GITHUB_TOKEN` - Automatically provided by GitHub Actions

### Optional Integrations
- **Slack/Teams Webhooks** - For release notifications
- **SonarQube** - Code quality analysis
- **Security Scanning Tools** - Enhanced vulnerability detection

## 🚀 Usage

### Triggering a Release
1. Update version in `Properties/AssemblyInfo.cs`
2. Update `VERSION.md` with release notes
3. Commit with message containing "Bump version"
4. Push to main branch
5. Workflow automatically creates GitHub release

### Manual Workflow Execution
```bash
# Trigger dependency check
gh workflow run dependency-management.yml

# View workflow status
gh run list --workflow=ci-cd.yml
```

## 📊 Status Badges

Add these badges to your README.md:

```markdown
[![CI/CD Pipeline](https://github.com/johanhenningsson4-hash/EMVReaderj/actions/workflows/ci-cd.yml/badge.svg)](https://github.com/johanhenningsson4-hash/EMVReaderj/actions/workflows/ci-cd.yml)
[![PR Validation](https://github.com/johanhenningsson4-hash/EMVReaderj/actions/workflows/pr-validation.yml/badge.svg)](https://github.com/johanhenningsson4-hash/EMVReaderj/actions/workflows/pr-validation.yml)
[![Security Scan](https://github.com/johanhenningsson4-hash/EMVReaderj/actions/workflows/ci-cd.yml/badge.svg?event=schedule)](https://github.com/johanhenningsson4-hash/EMVReaderj/actions/workflows/ci-cd.yml)
```

## 🔍 Monitoring

### Key Metrics Tracked
- ✅ Build success rate
- ✅ Test pass/fail rates  
- ✅ Security vulnerabilities
- ✅ Dependency freshness
- ✅ Release frequency

### Notifications
- **Build Failures** - Immediate notification via GitHub
- **Security Issues** - CodeQL alerts
- **Dependency Updates** - Weekly automated issues
- **Successful Releases** - Optional team notifications

## 🛠️ Troubleshooting

### Common Issues

**Build Failures:**
- Check .NET Framework 4.7.2 compatibility
- Verify NuGet package restoration
- Review MSBuild output logs

**Test Failures:**
- Check test assembly detection
- Verify VSTest runner availability
- Review test result outputs

**Release Issues:**
- Confirm version format in AssemblyInfo.cs
- Check commit message contains "Bump version"
- Verify GITHUB_TOKEN permissions

### Debugging Steps
1. Check workflow logs in Actions tab
2. Verify environment variables
3. Test locally with same build configuration
4. Review dependency versions

## 📝 Maintenance

### Regular Tasks
- **Monthly:** Review and update workflow versions
- **Quarterly:** Update GitHub Actions to latest versions
- **As Needed:** Adjust build configuration for new dependencies

### Workflow Updates
When updating workflows:
1. Test changes in a feature branch
2. Use workflow_dispatch for manual testing
3. Monitor first few automated runs
4. Document any configuration changes