import { HttpClient } from '@angular/common/http';
import { Injectable } from '@angular/core';
import {
  IPlatformCommandDto,
  IPlatformPagedResultDto,
  IPlatformRepositoryPagedQuery,
  IPlatformResultDto,
  PlatformApiService,
  PlatformAuthHttpRequestOptionsAppenderService,
  PlatformCommandDto,
  PlatformCoreModuleConfig,
  PlatformPagedQueryDto,
  PlatformPagedResultDto,
  PlatformResultDto,
} from '@platform-example-web/platform-core';
import { Observable } from 'rxjs';
import { map } from 'rxjs/operators';

import { AppsTextSnippetDomainModuleConfig } from '../apps-text-snippet-domain.config';
import { ITextSnippetDataModel, TextSnippetDataModel } from '../data-models';

@Injectable()
export class TextSnippetApi extends PlatformApiService {
  public constructor(
    moduleConfig: PlatformCoreModuleConfig,
    http: HttpClient,
    authHttpRequestOptionsAppender: PlatformAuthHttpRequestOptionsAppenderService,
    private domainModuleConfig: AppsTextSnippetDomainModuleConfig
  ) {
    super(moduleConfig, http, authHttpRequestOptionsAppender);
  }
  protected get apiUrl(): string {
    return `${this.domainModuleConfig.textSnippetApiHost}/api/TextSnippet`;
  }

  public search(query: SearchTextSnippetQueryDto): Observable<PlatformPagedResultDto<TextSnippetDataModel>> {
    return this.get<IPlatformPagedResultDto<ITextSnippetDataModel>>('/search', query).pipe(
      map(_ => {
        _.items = _.items.map(item => new TextSnippetDataModel(item));
        return new PlatformPagedResultDto(_);
      })
    );
  }

  public save(command: SaveTextSnippetCommand): Observable<SaveTextSnippetCommandResult> {
    return this.post<ISaveTextSnippetCommandResult>('/save', command).pipe(
      map(_ => new SaveTextSnippetCommandResult(_))
    );
  }
}

// ----------------- SearchTextSnippetQuery -------------------
export interface ISearchTextSnippetQuery extends IPlatformRepositoryPagedQuery {
  searchText?: string;
  searchId?: string;
}

export class SearchTextSnippetQueryDto extends PlatformPagedQueryDto implements ISearchTextSnippetQuery {
  public constructor(data?: ISearchTextSnippetQuery) {
    super(data);
    this.searchText = data?.searchText;
    this.searchId = data?.searchId;
  }
  public searchText?: string;
  public searchId?: string;
}

export interface ISaveTextSnippetCommand extends IPlatformCommandDto {
  data: ITextSnippetDataModel;
}

// ----------------- SaveTextSnippetCommand -------------------
export class SaveTextSnippetCommand extends PlatformCommandDto implements ISaveTextSnippetCommand {
  public constructor(data?: Partial<ISaveTextSnippetCommand>) {
    super();
    this.data = data?.data ?? new TextSnippetDataModel();
  }
  public data: ITextSnippetDataModel;
}

export interface ISaveTextSnippetCommandResult extends IPlatformResultDto {
  savedData: ITextSnippetDataModel;
}

export class SaveTextSnippetCommandResult extends PlatformResultDto implements ISaveTextSnippetCommandResult {
  public constructor(data?: ISaveTextSnippetCommandResult) {
    super();
    this.savedData = new TextSnippetDataModel(data?.savedData);
  }
  public savedData: TextSnippetDataModel;
}